using Microsoft.AspNetCore.SignalR;
using Server.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebServer.Models {
    public class File {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Time { get; set; }
    }

    public class Folder {
        public string Name { get; set; }
        public string Time { get; set; }
        public int Files { get; set; }
    }

    public class FileBreadcrumb {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsCurrent { get; set; } = false;
    }

    public class FolderInfo {
        public string Folder { get; set; }
        public List<FileBreadcrumb> Breadcrumbs { get; set; } = new List<FileBreadcrumb>();

        public List<Folder> Folders { get; set; } = new List<Folder>();
        public List<File> Files { get; set; } = new List<File>();
        public List<string> Removers { get; set; } = new List<string>();
    }

    public static class FolderBrowser {
        public static bool IsWaitApply = false;

        public static void CreateFolder(IClientProxy client, string dir, string root) {
            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (Directory.Exists(dir)) {
                return; // Already exists
            }

            Directory.CreateDirectory(dir);
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void RemoveFolder(IClientProxy client, string dir, string root) {
            var oDir = dir;
            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (!Directory.Exists(dir)) {
                return; // Not found
            }

            FileManager.RemoveRemover(oDir);

            Directory.Delete(dir, true);
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void RenameFolder(IClientProxy client, string dir, string newDir, string root) {
            if (newDir.IndexOf('\\') != -1 || newDir.IndexOf('/') != -1) {
                return; // Invalid format
            }

            var oDir = dir;
            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (!Directory.Exists(dir)) {
                return; // Not found
            }

            FileManager.RenameRemover(oDir, newDir);

            Directory.Move(dir, Path.Combine(dir, "../", newDir));
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void RemoveFile(IClientProxy client, string fpath, string root) {
            var gDir = Server.Config.Srv.DownloadFolder;
            fpath = Path.Combine(gDir, fpath);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(fpath));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (!System.IO.File.Exists(fpath)) {
                return; // Not found
            }

            System.IO.File.Delete(fpath);
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void RenameFile(IClientProxy client, string fpath, string name, string root) {
            if (name.IndexOf('\\') != -1 || name.IndexOf('/') != -1) {
                return; // Invalid format
            }

            var gDir = Server.Config.Srv.DownloadFolder;
            fpath = Path.Combine(gDir, fpath);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(fpath));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (!System.IO.File.Exists(fpath)) {
                return; // Not found
            }

            System.IO.File.Move(fpath, Path.Combine(fpath, "../", name));
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void SendFolder(IClientProxy client, string dir) {
            var oDir = dir;

            var gDir = Server.Config.Srv.DownloadFolder;
            dir = Path.Combine(gDir, dir);
            var check = Path.GetRelativePath(Path.GetFullPath(gDir), Path.GetFullPath(dir));
            if (check.StartsWith("..")) {
                return; // Backdoor detection
            }

            if (!Directory.Exists(dir)) {
                return; // Not found
            }

            var res = new FolderInfo {
                Folder = oDir
            };

            var split = oDir.Replace('\\', '/').Split('/');
            if (split.Length > 0 && !string.IsNullOrEmpty(split[0])) {
                for(int i = 0; i < split.Length; i++) {
                    res.Breadcrumbs.Add(new FileBreadcrumb {
                        Name = split[i],
                        Path = i == 0 ? split[0] : res.Breadcrumbs[i - 1].Path + "/" + split[i]
                    });
                }
                res.Breadcrumbs.Last().IsCurrent = true;
            }

            var files = Directory.GetFiles(dir);
            foreach(var item in files) {
                var info = new FileInfo(item);
                res.Files.Add(new File { 
                    Name = item.Substring(dir.Length + 1),
                    Size = info.Length,
                    Time = info.LastWriteTimeUtc.ToString("dd.MM.yy HH:mm:ss")
                });
                res.Files.Sort((x, y) => x.Name.CompareTo(y.Name));
            }

            var dirs = Directory.GetDirectories(dir);
            foreach(var item in dirs) {
                var info = new FileInfo(item);
                res.Folders.Add(new Folder {
                    Name = item.Substring(dir.Length + 1),
                    Time = info.LastWriteTimeUtc.ToString("dd.MM.yy HH:mm:ss"),
                    Files = Directory.GetFiles(item).Length
                });
                res.Folders.Sort((x, y) => x.Name.CompareTo(y.Name));
            }

            foreach(var r in FileManager.Remover) {
                if (string.IsNullOrEmpty(oDir)) {
                    var tok = r.Split('/');
                    if (tok.Length != 1)
                        continue;
                    res.Removers.Add(r);
                } else if (r.StartsWith(oDir)) {
                    var path = r.Substring(oDir.Length + 1);
                    var tok = path.Split('/');
                    if (tok.Length != 1)
                        continue;
                    res.Removers.Add(path);
                }
            }

            client.SendAsync("FolderBrowser", res);
        }

        public static void CreateRemover(IClientProxy client, string path, string root) {
            if (FileManager.Remover.Contains(path))
                return;

            var gDir = Server.Config.Srv.DownloadFolder;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(Path.Combine(gDir, dir))) {
                return;
            }

            if (System.IO.File.Exists(Path.Combine(gDir, path))) {
                return;
            }

            FileManager.Remover.Add(path);
            IsWaitApply = true;

            SendFolder(client, root);
        }

        public static void RemoveRemover(IClientProxy client, string path, string root) {
            if (!FileManager.Remover.Contains(path))
                return;

            FileManager.Remover.Remove(path);
            IsWaitApply = true;

            SendFolder(client, root);
        }
    }
}
