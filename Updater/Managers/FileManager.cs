using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ionic.Crc;
using PawnHunter.Numerals;

namespace Updater.Managers {
    public class FileCell {
        public string Path;
        public uint CRC;
        public ulong Size;
        public ulong Id;
        public string Url;

        public string AbsPath;
    }

    public static class FileManager {
        public static Dictionary<ulong, FileCell> Ids = new Dictionary<ulong, FileCell>();
        public static List<FileCell> Broken = new List<FileCell>();
        public static List<string> Remover = new List<string>();

        public static ulong BrokenSize {
            get {
                ulong size = 0;
                foreach (var f in Broken)
                    size += f.Size;
                return size;
            }
        }

        public static uint GetCRC(byte[] data) {
            var crc = new CRC32();
            using (var ms = new MemoryStream(data)) {
                return (uint)crc.GetCrc32(ms);
            }
        }

        public static uint GetCRC(string fpath) {
            var crc = new CRC32();
            using (var fs = File.OpenRead(fpath)) {
                return (uint)crc.GetCrc32(fs);
            }
        }

        public static void CheckFiles() {
            Parallel.ForEach(Ids, (kv) => {
                EntryPoint.Window.AddLineAsync($"Проверка \"{kv.Value.AbsPath}\"");
                var fpath = kv.Value.Path;
                var isApp = fpath == EntryPoint.ExeName;
                if (isApp) {
                    fpath = AppDomain.CurrentDomain.FriendlyName;
                    kv.Value.AbsPath = EntryPoint.ExeName + ".dw";
                }
                var info = new FileInfo(fpath);
                //if (DownloadManager.Current != null && DownloadManager.Current.Id == kv.Value.Id
                //    && DownloadManager.Current.CRC != kv.Value.CRC) {
                //    EntryPoint.Tcp.Session.Send(new DownloadSeek { FileId = kv.Value.Id, FilePos = 0 });
                //}
                if (!info.Exists || info.Length != (long)kv.Value.Size || GetCRC(fpath) != kv.Value.CRC) {
                    if (!Broken.Exists(x => x.Id == kv.Value.Id)) {
                        if (isApp) {
                            lock (Broken) Broken.Insert(0, kv.Value);
                        } else {
                            lock (Broken) Broken.Add(kv.Value);
                        }
                    }
                }
            });
            if (Broken.Count != 0) {
                EntryPoint.Window.CurMode = Mode.CanDownload;
                FormattableString fmt = $"{Broken.Count:W;Доступен,Доступно} для скачивания {Broken.Count} {Broken.Count:W;файл,файла,файлов}";
                EntryPoint.Window.AddLineAsync(fmt.ToString(new NumeralsFormatter()));
                EntryPoint.Window.Dispatcher.Invoke(() => {
                    if (EntryPoint.Window.WindowState == System.Windows.WindowState.Minimized || !EntryPoint.Window.IsVisible) {
                        EntryPoint.Tray.ShowBalloonTipFor(2000, "Updater", "Доступно обновление", System.Windows.Forms.ToolTipIcon.Info, () => {
                            EntryPoint.Window.Show();
                        });
                    }
                });
            } else {
                EntryPoint.Window.CurMode = Mode.CanPlay;
            }
        }
    }
}
