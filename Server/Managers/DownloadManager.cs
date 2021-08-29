using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Managers {
    public class DownloadManager {
        public Session Session;
        public HashSet<ulong> Downloads = new HashSet<ulong>();

        public static bool IsLocked = false;

        public static void Lock() {
            IsLocked = true;
            lock (Session.Clients) {
                foreach (var client in Session.Clients) {
                    client.Downloader.LockClient();
                }
            }

            if (OperatingSystem.IsLinux()) {
                "systemctl stop nginx".Bash();
            }
        }

        public static void Unlock() {
            IsLocked = false;
            lock (Session.Clients) {
                foreach (var client in Session.Clients) {
                    client.Downloader.UnlockClient();
                }
            }

            if (OperatingSystem.IsLinux()) {
                "systemctl start nginx".Bash();
            }
        }

        public void LockClient() {
            lock (Downloads) {
                Downloads.Clear();
                Session.Send(new ServerPackets.DownloadLockable { IsLocked = true });
            }
        }

        public void UnlockClient() {
            Session.Send(new ServerPackets.DownloadLockable { IsLocked = false });
        }

        public DownloadManager(Session session) {
            Session = session;
        }

        public void DownloadStarted(ClientPackets.DownloadStarted packet) {
            lock(Downloads) {
                if (Downloads.Contains(packet.FileId)) {
                    return;
                }
                Downloads.Add(packet.FileId);
            }
        }

        public void DownloadCanceled(ulong fileId) {
            lock (Downloads) {
                if (!Downloads.Contains(fileId))
                    return;
                Downloads.Remove(fileId);
            }
        }
    }
}
