using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SystemConfig;
using Updater.Network;

namespace Updater.Managers {
    public class DownloadItem {
        public ulong Id;
        public string Path;
        public uint CRC;
        public FileStream Stream;

        public WebDownload Download;

        public void CloseStream() {
            Stream.Close();
            Stream.Dispose();

            if (Download != null) {
                Download.Dispose();
            }
        }
    }

    public static class DownloadManager {
        public static DownloadItem Current { get; set; }
        public static DownloadInfo Loaded;

        private static bool _alreadyRun;
        private static bool _errored;

        private static readonly object Locker = new object();

        public static void PartCatcher() {
            if (_alreadyRun)
                return;
            Task.Run(() => {
                lock (Locker) {
                    if (Current == null) {
                        _alreadyRun = false;
                        return;
                    }
                    if (EntryPoint.Window.Downloader.IsPaused) {
                        _alreadyRun = false;
                        return;
                    }
                    try {
                        if (_errored) {
                            Current.Download.StartDownload(Current.Stream.Position);
                            _errored = false;
                        }
                        var ready = Current.Download.GetPart();
                        if (ready.Readed <= 0) {
                            OnFinish();
                        } else {
                            OnPart(ready.Buffer, ready.Readed);
                        }
                    } catch (System.Net.WebException ex) {
                        EntryPoint.Window.Downloader.IsPaused = true;
                        EntryPoint.Window.SetPaused(true);
                        EntryPoint.Window.AddLineAsync($"Ошибка во время получения файла \"{ex.Message}\"");
                        _errored = true;
                    } catch (Exception ex) {
                        EntryPoint.Window.AddLineAsync($"Критическая ошибка во время получения файла \"{ex.Message}\"");
                        EntryPoint.Window.SetFinished();
                        EntryPoint.Window.Downloader.IsPaused = false;
                        EntryPoint.Window.CurMode = Mode.CanDownload;
                        Loaded = null;
                        Current = null;
                        _errored = false;
                        _alreadyRun = false;
                        return;
                    }
                }
                _alreadyRun = false;
                PartCatcher();
            });
        }

        public static void StartDownload(FileCell cell) {
            lock (Locker) {
                if (Current != null)
                    return;

                long pos = 0;
                if (Loaded != null && Loaded.Id == cell.Id) {
                    using (var fs = File.OpenRead(Loaded.Path)) {
                        fs.Seek(0, SeekOrigin.End);
                        pos = fs.Position;
                    }
                }

                Start(cell.Id, pos);
            }
        }

        public static void ResumeDownload() {
            if (Current == null)
                return;

            EntryPoint.Tcp.Send(new ClientPackets.DownloadStarted { FileId = Current.Id });
            PartCatcher();
        }

        public static void Start(ulong fileId, long filePos) {
            var cell = FileManager.Ids[fileId];

            FileStream fs;
            if (Loaded != null && Loaded.Id == cell.Id) {
                fs = File.OpenWrite(cell.AbsPath);
                fs.Seek(0, SeekOrigin.End);
            } else {
                var dir = Path.GetDirectoryName(cell.AbsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                fs = File.Create(cell.AbsPath);
            }

            Load(fileId, filePos, cell, fs);

            ulong total = 0;
            FileManager.Broken.ForEach(x => { total += x.Size; });
            var panel = EntryPoint.Window.Downloader;
            panel.GlobalSize = 0;
            panel.TotalGlobalSize = total;
            panel.Size = 0;
            panel.TotalSize = cell.Size;
            panel.File = cell.AbsPath;
            panel.Count = 1;
            panel.TotalCount = FileManager.Broken.Count;

            if (Loaded != null && Loaded.Id == cell.Id) {
                var pos = fs.Position;
                panel.Size += (ulong)pos;
                panel.GlobalSize += (ulong)pos;
            }

            PartCatcher();
        }

        private static void Load(ulong fileId, long filePos, FileCell cell, FileStream fs) {
            try {
                lock (Locker) Current = new DownloadItem {
                    Id = fileId,
                    CRC = cell.CRC,
                    Path = cell.AbsPath,
                    Stream = fs,
                    Download = new WebDownload(cell.Url, filePos)
                };
            } catch (System.Net.WebException ex) {
                EntryPoint.Window.AddLineAsync($"Ошибка во время получения файла \"{cell.Path}\" - \"{ex.Message}\"");
                EntryPoint.Window.SetFinished();
                EntryPoint.Window.Downloader.IsPaused = false;
                EntryPoint.Window.CurMode = Mode.CanDownload;
                Loaded = null;
                Current = null;
                fs.Dispose();
                fs.Close();
                return;
            }

            EntryPoint.Tcp.Send(new ClientPackets.DownloadStarted { FileId = Current.Id });
            EntryPoint.Window.AddLineAsync($"Загружается \"{cell.AbsPath}\"");
            EntryPoint.Window.CurMode = Mode.Download;
            EntryPoint.Window.SetStarted();
        }

        private static void OnFinish() {
            if (Loaded != null && Loaded.Id == Current.Id)
                Loaded = null;

            // Notify server
            EntryPoint.Tcp.Send(new ClientPackets.DownloadFinish { FileId = Current.Id });

            lock (Locker) {
                Current.CloseStream();
                // Self-updating
                if (Current.Path.EndsWith(".dw")) {
                    var cur = AppDomain.CurrentDomain.FriendlyName;
                    File.Move(cur, EntryPoint.ExeName + ".old");
                    File.Move(Current.Path, cur);
                    Process.Start(cur, $"{Process.GetCurrentProcess().Id}");
                    Current = null;
                    EntryPoint.Window.Dispatcher.Invoke(() => {
                        EntryPoint.Window.FullClose();
                    });
                    return;
                }
                Current = null;
            }

            FileManager.Broken.RemoveAt(0);
            if (FileManager.Broken.Count == 0) {
                EntryPoint.Window.SetFinished();
                EntryPoint.Window.AddLineAsync("Проверка после скачивания");
                FileManager.CheckFiles();
                return;
            }

            var cell = FileManager.Broken.First();
            var panel = EntryPoint.Window.Downloader;
            panel.Size = 0;
            panel.TotalSize = cell.Size;
            panel.Count++;
            panel.File = cell.AbsPath;

            long pos = 0;
            FileStream fs;
            if (Loaded != null && Loaded.Id == cell.Id) {
                fs = File.OpenWrite(cell.AbsPath);
                fs.Seek(0, SeekOrigin.End);
                pos = fs.Position;
                panel.Size += (ulong)pos;
                panel.GlobalSize += (ulong)pos;
            } else {
                var dir = Path.GetDirectoryName(cell.AbsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                fs = File.Create(cell.AbsPath);
            }

            // Start next
            Load(cell.Id, pos, cell, fs);

            PartCatcher();
        }

        private static DateTime _speedStamp = DateTime.Now;
        private static int _speedCalcer;

        public static void OnPart(byte[] buffer, int size) {
            var panel = EntryPoint.Window.Downloader;
            Current.Stream.Write(buffer, 0, size);
            panel.Size += (ulong)size;
            panel.GlobalSize += (ulong)size;

            EntryPoint.Window.SetProgress(panel.GlobalSize / (double)panel.TotalGlobalSize);

            _speedCalcer += size;
            if ((DateTime.Now - _speedStamp).TotalSeconds > 1) {
                EntryPoint.Window.Downloader.Speed = _speedCalcer;
                _speedCalcer = 0;
                _speedStamp = DateTime.Now;
            }
        }

        public static void OnCancel() {
            if (Loaded != null && Loaded.Id == Current.Id)
                Loaded = null;
            lock (Locker) {
                EntryPoint.Tcp.Send(new ClientPackets.DownloadCanceled { FileId = Current.Id });
                Current.CloseStream();
                File.Delete(Current.Path);
                Current = null;
            }
            EntryPoint.Window.SetFinished();
            EntryPoint.Window.Downloader.IsPaused = false;
            EntryPoint.Window.CurMode = Mode.CanDownload;
            EntryPoint.Window.AddLineAsync("Скачивание отменено");
        }
    }
}
