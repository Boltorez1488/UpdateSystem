using Joveler.Compression.ZLib;
using NLog;
using Shared.Opcodes;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server {
    public class Program {
        static AutoResetEvent ExitEvent = new AutoResetEvent(false);
        public static Logger Log = LogManager.GetLogger("InfoLog");

        public static void InitNativeLibrary() {
#if DEBUG
            string libDir = "runtimes";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libDir = Path.Combine(libDir, "win-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libDir = Path.Combine(libDir, "linux-");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libDir = Path.Combine(libDir, "osx-");

            switch (RuntimeInformation.ProcessArchitecture) {
                case Architecture.X86:
                    libDir += "x86";
                    break;
                case Architecture.X64:
                    libDir += "x64";
                    break;
                case Architecture.Arm:
                    libDir += "arm";
                    break;
                case Architecture.Arm64:
                    libDir += "arm64";
                    break;
            }
            libDir = Path.Combine(libDir, "native");
#else
            string libDir = "";
#endif

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libPath = Path.Combine(libDir, "zlibwapi.dll");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libPath = Path.Combine(libDir, "libz.so");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libPath = Path.Combine(libDir, "libz.dylib");

            if (libPath == null)
                throw new PlatformNotSupportedException($"Unable to find native library.");
            if (!File.Exists(libPath))
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");

            ZLibInit.GlobalInit(libPath);
        }

        static Server server;

        public static void Start() {
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("cfg/NLog.config");
            InitNativeLibrary();

            OpcodeIO.Start("Server.Session", "ServerPackets");

            Config.Load();
            Log.Info("Configuration loaded");

            Managers.FileManager.Load();
            Managers.FileManager.LoadRemover();

#if DEBUG
            server = new Server(IPAddress.Any, Config.Srv.Port);
#else
            server = new Server(IPAddress.Any, Config.Srv.Port);
#endif
            server.Start();
            Log.Info($"Server started on {Config.Srv.Port} port");
        }

        public static void Stop() {
            server.Stop();
            Managers.FileManager.SaveRemover();
            Log.Info("Server stopped");
        }

        static void Main(string[] args) {
            Start();
            Console.CancelKeyPress += (sender, eventArgs) => {
                ExitEvent.Set();
                eventArgs.Cancel = true;
            };
            ExitEvent.WaitOne();
            Stop();
        }
    }
}
