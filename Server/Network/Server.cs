using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using System.Threading;
using NLog;

namespace Server {
    public class Server : TcpServer {
        public Logger Log = LogManager.GetLogger("Server");

        public Server(IPAddress address, int port) : base(address, port) {}

        protected override TcpSession CreateSession() { return new Session(this); }

        protected override void OnError(SocketError error) {
            Log.Fatal($"Server error with code {error}");
        }

        public void Updater() {
            while(IsStarted) {
                Thread.Sleep(1000);
                if (!IsStarted)
                    break;

                List<Session> kicks = new List<Session>();
                lock (Session.Clients) {
                    foreach(var client in Session.Clients) {
                        if (!client.IsClientAlive)
                            kicks.Add(client);
                    }
                }
                foreach(var client in kicks) {
                    client.Disconnect();
                }

                foreach(var kv in Session.IpStamps) {
                    if ((DateTime.UtcNow - kv.Value).TotalSeconds > 2) {
                        Session.IpStamps.TryRemove(kv.Key, out _);
                    }
                }
            }
        }
    }
}
