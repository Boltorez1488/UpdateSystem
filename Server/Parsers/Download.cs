using System;
using System.Collections.Generic;
using System.Text;

namespace Server {
    public partial class Session {
        public void OnDownloadStarted(ClientPackets.DownloadStarted packet) {
            Downloader.DownloadStarted(packet);
        }

        public void OnDownloadFinish(ClientPackets.DownloadFinish packet) {
            Downloader.DownloadCanceled(packet.FileId);
        }

        public void OnDownloadCanceled(ClientPackets.DownloadCanceled packet) {
            Downloader.DownloadCanceled(packet.FileId);
        }
    }
}
