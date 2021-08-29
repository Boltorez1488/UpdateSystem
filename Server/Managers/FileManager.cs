using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Joveler.Compression.ZLib.Checksum;

namespace Server.Managers {
    public class FMInfo {
        public ulong Id;
        public ulong Size;
        public uint CRC;

        // UTC
        public DateTime LastWriteTime;
        public string AbsPath;
    }

    public class FMCell {
        public string Path;
        public FMInfo Info;
    }

    public static class FileManager {
        public static Dictionary<string, FMInfo> Files = new Dictionary<string, FMInfo>();
        public static Dictionary<ulong, FMCell> Ids = new Dictionary<ulong, FMCell>();
        //public static Dictionary<ulong, FMInfo> IdToFMI = new Dictionary<ulong, FMInfo>();
        static ulong gId = 0;

        public static HashSet<string> Remover = new HashSet<string>();

        private static uint GetCRC(string fpath) {
            var crc = new Crc32Checksum();
            using (var fs = File.OpenRead(fpath)) {
                crc.Append(fs);
                return crc.Checksum;
            }
        }

        public static void Reload() {
            var res = new ServerPackets.CrcUpdate();

            var dir = Config.Srv.DownloadFolder;
            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (var f in files) {
                var info = new FileInfo(f);
                if (info.Attributes.HasFlag(FileAttributes.Hidden)) {
                    continue;
                }
                var spath = f.Substring(dir.Length + 1).Replace('\\', '/');
                if (Files.ContainsKey(spath)) {
                    if (Files[spath].LastWriteTime >= info.LastWriteTimeUtc)
                        continue;

                    var fi = Files[spath];
                    var crc = GetCRC(f);
                    if (fi.CRC == crc)
                        continue;

                    fi.Size = (ulong)info.Length;
                    fi.CRC = crc;
                    fi.LastWriteTime = info.LastWriteTimeUtc;
                    res.Updated.Add(new ServerPackets.CrcList.Types.CrcItem {
                        Crc = crc,
                        FileId = fi.Id,
                        Path = spath,
                        Size = fi.Size
                    });
                } else {
                    var id = gId++;
                    var crc = GetCRC(f);
                    var size = (ulong)info.Length;
                    var fm = Files[spath] = new FMInfo {
                        Id = id,
                        CRC = crc,
                        Size = size,
                        LastWriteTime = info.LastWriteTimeUtc,
                        AbsPath = f.Replace('\\', '/')
                    };
                    Ids[id] = new FMCell { Path = spath, Info = fm };
                    res.Updated.Add(new ServerPackets.CrcList.Types.CrcItem {
                        Crc = crc,
                        FileId = id,
                        Path = spath,
                        Size = size
                    });
                    //IdToFMI[id] = Files[spath];
                    //res.refresh.Add(new ServerPackets.CrcItem(spath, crc, size, id));
                }
            }

            foreach(var kv in Files) {
                if (File.Exists(kv.Value.AbsPath)) {
                    continue;
                }
                if (Remover.Contains(kv.Key)) {
                    Remover.Remove(kv.Key);
                }
                var fi = kv.Value;
                Files.Remove(kv.Key);
                Ids.Remove(fi.Id);
                //IdToFMI.Remove(fi.Id);
                //res.removed.Add(new ServerPackets.CrcItem(kv.Key, fi.CRC, fi.Size, fi.Id));
                res.Removed.Add(new ServerPackets.CrcList.Types.CrcItem {
                    Crc = fi.CRC,
                    FileId = fi.Id,
                    Path = kv.Key,
                    Size = fi.Size
                });
            }

            if (res.Updated.Count == 0 && res.Removed.Count == 0)
                return;

            lock (Session.Clients) {
                foreach (var client in Session.Clients) {
                    try {
                        client.Send(res);
                    } catch (Exception ex) {
                        client.Log.Fatal(ex, $"{ex}, when files reload");
                    }
                }
            }
        }

        public static void RenameRemover(string dir, string newDir) {
            var list = new List<string>();
            var rem = new List<string>();
            foreach (var r in Remover) {
                if (r.StartsWith(dir)) {
                    var path = r.Substring(dir.Length + 1);
                    var tok = path.Split('/');
                    if (tok.Length != 1)
                        continue;
                    list.Add(Path.Combine(newDir, path).Replace('\\', '/'));
                    rem.Add(r);
                }
            }
            foreach (var r in rem)
                Remover.Remove(r);
            foreach (var r in list)
                Remover.Add(r);
        }

        public static void RemoveRemover(string dir) {
            var rem = new List<string>();
            foreach (var r in Remover) {
                if (r.StartsWith(dir)) {
                    rem.Add(r);
                }
            }
            foreach (var r in rem)
                Remover.Remove(r);
        }

        public static void RemoverCheck() {
            var gDir = Config.Srv.DownloadFolder;
            var list = new List<string>();
            foreach(var r in Remover) {
                if (File.Exists(Path.Combine(gDir, r)))
                    list.Add(r);
            }
            foreach(var r in list) {
                Remover.Remove(r);
            }

            if (Remover.Count != 0) {
                var rem = new ServerPackets.FilesRemove();
                rem.Pathes.AddRange(Remover);
                lock (Session.Clients) {
                    foreach (var client in Session.Clients) {
                        try {
                            client.Send(rem);
                        } catch (Exception ex) {
                            client.Log.Fatal(ex, $"{ex}, when remover send");
                        }
                    }
                }
            }
        }

        public static void Load() {
            var dir = Config.Srv.DownloadFolder;
            if (string.IsNullOrEmpty(dir)) {
                Program.Log.Fatal("FileManager error, directory = null");
                Environment.Exit(0x0);
                return;
            }
            if (!Directory.Exists(dir)) {
                Program.Log.Warn($"FileManager '{dir}' not found, creating");
                Directory.CreateDirectory(dir);
            }

            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach(var f in files) {
                var info = new FileInfo(f);
                if (info.Attributes.HasFlag(FileAttributes.Hidden)) {
                    continue;
                }
                var spath = f.Substring(dir.Length + 1).Replace('\\', '/');
                var crc = new Crc32Checksum();
                using (var fs = File.OpenRead(f)) {
                    crc.Append(fs);
                    var id = gId++;
                    var fm = Files[spath] = new FMInfo { 
                        Id = id,
                        CRC = crc.Checksum,
                        Size = (ulong)info.Length,
                        LastWriteTime = info.LastWriteTimeUtc,
                        AbsPath = f.Replace('\\', '/')
                    };
                    Ids[id] = new FMCell { Path = spath, Info = fm };
                    //IdToFMI[id] = Files[spath];
                }
            }

            Program.Log.Info($"FileManager loaded {Files.Count} files");
        }

        public static void SaveRemover() {
            if (Remover.Count == 0) {
                if (File.Exists("cfg/remover.xml"))
                    File.Delete("cfg/remover.xml");
                return;
            }
            var xDoc = new XDocument();
            var root = new XElement("FileRemover");
            foreach(var r in Remover) {
                root.Add(new XElement("File", r));
            }
            xDoc.Add(root);
            xDoc.Save("cfg/remover.xml");
            Program.Log.Info($"FileManager remover saved {Remover.Count} cells");
        }

        public static void LoadRemover() {
            if (!File.Exists("cfg/remover.xml"))
                return;
            var xDoc = XDocument.Load("cfg/remover.xml");
            if (xDoc.Root.Name.LocalName != "FileRemover")
                return;

            Remover.Clear();
            var gDir = Config.Srv.DownloadFolder;
            foreach (var el in xDoc.Root.Elements()) {
                if (el.Name.LocalName != "File")
                    continue;
                var dir = Path.GetDirectoryName(el.Value);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(gDir, dir))) {
                    continue;
                }
                if (!Files.ContainsKey(el.Value))
                    Remover.Add(el.Value);
            }

            Program.Log.Info($"FileManager remover loaded {Remover.Count} cells");
        }
    }
}
