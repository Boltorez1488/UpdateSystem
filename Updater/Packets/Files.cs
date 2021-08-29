using System;
using System.Linq;
using PawnHunter.Numerals;
using Updater.Managers;

namespace Updater.Socket {
    public partial class Session {
        public void OnCrcResponse(ServerPackets.CrcList packet) {
            foreach(var item in packet.Items) {
                FileManager.Ids[item.FileId] = new FileCell {
                    Id = item.FileId,
                    CRC = item.Crc,
                    Size = item.Size,
                    Path = item.Path,
                    Url = item.Url,
                    AbsPath = item.Path
                };
            }
            var count = packet.Items.Count;
            FormattableString fmt = $"{count:W;Получен,Получено} {count} {count:W;файл,файла,файлов} на проверку";
            EntryPoint.Window.AddLineAsync(fmt.ToString(new NumeralsFormatter()));
            if (DownloadManager.Current == null) {
                FileManager.CheckFiles();
            }
        }

        public void OnCrcUpdate(ServerPackets.CrcUpdate packet) {
            foreach(var item in packet.Updated) {
                FileManager.Ids[item.FileId] = new FileCell {
                    Id = item.FileId,
                    CRC = item.Crc,
                    Size = item.Size,
                    Path = item.Path,
                    Url = item.Url,
                    AbsPath = item.Path
                };
            }
            foreach(var item in packet.Removed) {
                FileManager.Ids.Remove(item.FileId);
                FileManager.Broken.RemoveAll(x => x.Id == item.FileId);
            }
            var count = packet.Updated.Count;
            if (count == 0)
                return;
            FormattableString fmt = $"{count:W;Обновлен,Обновлено} {count} {count:W;файл,файла,файлов}";
            EntryPoint.Window.AddLineAsync(fmt.ToString(new NumeralsFormatter()));
            if (DownloadManager.Current == null) {
                FileManager.CheckFiles();
            }
        }

        public void OnFileRemover(ServerPackets.FilesRemove packet) {
            FileManager.Remover = packet.Pathes.ToList();
            //Parallel.ForEach(packet.Pathes, (path) => {
            //    try {
            //        if (File.Exists(path))
            //            File.Delete(path);
            //        else if (Directory.Exists(path))
            //            Directory.Delete(path);
            //    } catch (Exception) {
            //        //...
            //    }
            //});
        }
    }
}
