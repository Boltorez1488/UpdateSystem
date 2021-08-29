using ClientPackets;
using Google.Protobuf;
using System;
using Updater.Managers;

namespace Updater.Socket {
    public partial class Session {
        public Client Socket { get; }
        public PacketsWorker Worker;

        public Session(Client socket) {
            Socket = socket;
            Worker = new PacketsWorker(this);
        }

        public void OnCreated() {
            Send(new CrcRequest());

            EntryPoint.Window.IsEnabledDownload = true;
            DownloadManager.ResumeDownload();
        }

        public void OnReceived(byte[] buffer, long offset, long size) {
            try {
                Worker.ReadData(buffer, offset, size);
            } catch (Exception ex) {
                EntryPoint.Window.AddLineAsync($"(Пакеты) Получено исключение: {ex}");
                Socket.Reconnect();
            }
        }

        public bool Send(IMessage packet) {
            var res = Shared.Network.PacketParser.PackPacket(packet);
            if (res == null)
                return false;
            Socket.Socket.SendAsync(res);
            return true;
        }
    }
}
