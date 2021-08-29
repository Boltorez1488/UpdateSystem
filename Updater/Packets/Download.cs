using Updater.Managers;

namespace Updater.Socket {
    public partial class Session {
        //public void OnDownloadStart(ServerPackets.DownloadPart packet) {
        //    if (DownloadManager.Current == null) {
        //        DownloadManager.OnStarted(packet);
        //    }
        //    DownloadManager.OnPart(packet);
        //}

        public void OnDownloadLocked(ServerPackets.DownloadLockable packet) {
            EntryPoint.Window.IsEnabledDownload = !packet.IsLocked;
            if (packet.IsLocked && DownloadManager.Current != null) {
                DownloadManager.OnCancel();
            }
        }
    }
}
