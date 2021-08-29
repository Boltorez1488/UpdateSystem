using Google.Protobuf;
using System;
using System.Threading.Tasks;
using Cave.Net;

namespace Updater.Socket {
    public class Client : IDisposable {
        public TcpAsyncClient Socket { get; private set; }

        ~Client() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;
        protected virtual void Dispose(bool disposing) {
            if (isDisposed) return;
            if (disposing && Socket != null) {
                Socket.Dispose();
            }
            isDisposed = true;
        }

        public bool Send(IMessage packet) {
            return Session != null && Session.Send(packet);
        }

        //====================================[MANAGEMENT]====================================

#if DEBUG
        public string Host = "127.0.0.1";
        public int Port = 1111;
#else
		public string Host = "127.0.0.1";
        public int Port = 1111;
#endif

        public Session Session { get; private set; }

        public Client() {
            InitSocket();
        }

        private void InitSocket() {
            Socket = new TcpAsyncClient();
            Socket.Connected += (s, e) => {
                EntryPoint.Window.LastError = "";
                Session = new Session(this);
                EntryPoint.Window.IsConnected = true;
                Session.OnCreated();
            };
            Socket.Disconnected += (s, e) => {
                Session = null;
                EntryPoint.Window.IsConnected = false;
                Reconnect();
            };
            Socket.Received += Socket_Received;
            Socket.Error += Socket_Error;
        }

        private void Socket_Error(object sender, Cave.IO.ExceptionEventArgs e) {
            EntryPoint.Window.LastError = $"{e.Exception.Message}";
            Reconnect();
        }

        private void Socket_Received(object sender, BufferEventArgs e) {
            Session.OnReceived(e.Buffer, e.Offset, e.Length);
            Socket.ReceiveBuffer.Clear();
        }

        public void Start() {
            Socket.ConnectAsync(Host, Port);
            Alive();
        }

        public Task Reconnect() {
            return Task.Run(async () => {
                Socket.Close();
                Socket.Dispose();
                await Task.Delay(1000);
                InitSocket();
                Socket.Connect(Host, Port);
            });
        }

        public void Alive() {
            Task.Run(async () => {
                if (Session == null || Socket == null) {
                    await Task.Delay(1000);
                    Alive();
                    return;
                }
                Session.Send(new ClientPackets.AliveTick());
                await Task.Delay(5000);
                Alive();
            });
        }
    }
}
