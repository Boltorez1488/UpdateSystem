using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using NetCoreServer;
using NLog;

namespace Server {
    public partial class Session : TcpSession {
        //private static int _count = 0;
        public static int Count => Clients.Count;

        public IPAddress Ip;

        public static ConcurrentDictionary<IPAddress, DateTime> IpStamps = new();
        public static ConcurrentDictionary<IPAddress, Session> Ips = new();

        public bool IsCheckConnector;
        public static HashSet<Session> Clients = new();

        public PacketsWorker Worker;
        public Managers.DownloadManager Downloader;

        public Logger Log = LogManager.GetLogger("Session").WithProperty("ip", null);

        public DateTime LastAliveTick = DateTime.UtcNow;
        public bool IsClientAlive {
            get {
                var diff = DateTime.UtcNow - LastAliveTick;
                return diff.TotalSeconds < Config.Srv.AliveTimeout;
            }
        }

        public Session(TcpServer server) : base(server) {
            Downloader = new Managers.DownloadManager(this);
            Worker = new PacketsWorker(this);
        }

        public IPEndPoint EndPoint => (IPEndPoint)Socket.RemoteEndPoint;

        public IPAddress GetRemoteIp() {
            return EndPoint?.Address;
        }

        public ushort GetRemotePort() {
            return (ushort)EndPoint.Port;
        }

        protected void AliveChecker() {
            Task.Run(async () => {
                await Task.Delay(1000);
                if (!IsClientAlive) {
                    Disconnect();
                    return;
                }
                AliveChecker();
            }, tokenSrc.Token);
        }

        public delegate void ClientConnectedEvent(Session caller);
        public delegate void ClientDisconnectedEvent(Session caller);
        public static event ClientConnectedEvent OnClientConnect;
        public static event ClientDisconnectedEvent OnClientDisconnect;

        protected override void OnConnected() {
            Ip = GetRemoteIp();
#if !DEBUG
            if (IpStamps.ContainsKey(Ip)) {
                if (IpStamps.TryGetValue(Ip, out var dt) && (DateTime.UtcNow - dt).TotalSeconds < 1) {
                    Disconnect();
                    return;
                }
            }
            IpStamps.TryAdd(Ip, DateTime.UtcNow);

            if (Ips.ContainsKey(Ip)) {
                Disconnect();
                return;
            }
            Ips.TryAdd(Ip, this);
#endif

            lock (Clients) {
                Clients.Add(this);
                //Interlocked.Increment(ref _count);
            }

            IsCheckConnector = true;

            AliveChecker();

            Log.SetProperty("ip", $"[{Ip}]");
            Log.Trace("Connected");

            OnClientConnect?.Invoke(this);

            if (Managers.DownloadManager.IsLocked) {
                Send(new ServerPackets.DownloadLockable { IsLocked = true });
            }
        }

        protected override void OnDisconnected() {
            if (!IsCheckConnector)
                return;

            tokenSrc.Cancel();
            Ips.TryRemove(Ip, out _);

            lock (Clients) {
                Clients.Remove(this);
                //Interlocked.Decrement(ref _count);
            }
            Log.Trace("Disconnected");
            Log.SetProperty("ip", null);

            OnClientDisconnect?.Invoke(this);
        }

        private readonly CancellationTokenSource tokenSrc = new();
        protected void RunRemain() {
            Task.Run(() => {
                try {
                    Worker.ReadRemain();
                    if (Worker.NeedReadRemainBuffer) {
                        RunRemain();
                    }
                } catch (Exception ex) {
                    Log.Fatal(ex, $"{ex} Connection close");
                    Disconnect();
                }
            }, tokenSrc.Token);
        }

        protected override void OnReceived(byte[] buffer, long offset, long size) {
            try {
                Worker.ReadData(buffer, offset, size);
                if (Worker.NeedReadRemainBuffer) {
                    RunRemain();
                }
            } catch (SpamException ex) {
                Log.Fatal(ex, $"{ex}, Client kicked");
                //Bans.SpamDetect(GetRemoteIpAddress());
                Disconnect();
            } catch (Exception ex) {
                Log.Fatal(ex, $"{ex}, Client kicked");
                Disconnect();
            }
        }

        protected override void OnError(SocketError error) {
            Log.Fatal($"TCP error with code {error}");
            //Console.WriteLine($"Chat TCP session caught an error with code {error}");
        }

        public bool Send(IMessage packet) {
            var res = Shared.Network.PacketParser.PackPacket(packet);
            if (res == null)
                return false;
            return Send(res) > 0;
        }
    }
}
