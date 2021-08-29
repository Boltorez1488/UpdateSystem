using Google.Protobuf;
using Shared.Opcodes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using PeanutButter.TrayIcon;
using Updater.Managers;
using Updater.Socket;

namespace Updater {
    public static class EntryPoint {
        public static string ExeName => "Updater.exe";

        private static bool Is47Installed() {
            // API changes in 4.7: https://github.com/Microsoft/dotnet/blob/master/releases/net47/dotnet47-api-changes.md
            return Type.GetType("System.Web.Caching.CacheInsertOptions, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false) != null;
        }

        static Mutex _protectMultiWindow;

        [STAThread]
        public static void Main(string[] args) {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                Exception ex = (Exception)e.ExceptionObject;
                MessageBox.Show($"{ex}", "Необработанное исключение", MessageBoxButton.OK, MessageBoxImage.Warning);

                Tray.Dispose();
#if !DEBUG
                Environment.Exit(0x0);
#endif
            };

            if (args.Length > 0) {
                try {
                    var pid = args[0];
                    var proc = Process.GetProcessById(int.Parse(pid));
                    proc.WaitForExit(15000);
                } catch (Exception) {
                    //...
                }
            }

            if (File.Exists(ExeName + ".old")) {
                File.Delete(ExeName + ".old");
            }

            if (Mutex.TryOpenExisting("$Game_Updater$", out _protectMultiWindow)) {
                return;
            }
            _protectMultiWindow = new Mutex(true, "$Game_Updater$");

            try {
                var frameworkName = new System.Runtime.Versioning.FrameworkName(
                    AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName
                );
                if (frameworkName.Version < new Version(4, 8)) {
                    MessageBox.Show("Для запуска необходим .Net Framework v4.8");
                    return;
                }
            } catch(Exception) {
                MessageBox.Show("Для запуска необходим .Net Framework v4.8");
                return;
            }

            OpcodeIO.Start("Updater.Socket.Session", "ClientPackets");
            Tcp = new Client();

            var app = new App();
            app.InitializeComponent();
            app.Run();

            Tcp.Dispose();
            Tray.Dispose();
        }

        public static void Save() {
            var cfg = new SystemConfig.Config {
                Win = new SystemConfig.WinParams {
                    W = (int) Window.Width,
                    H = (int) Window.Height,
                    Maximized = Window.WindowState == WindowState.Maximized
                }
            };
            if (DownloadManager.Current != null) {
                DownloadManager.Current.CloseStream();
                cfg.Download = new SystemConfig.DownloadInfo {
                    Id = DownloadManager.Current.Id,
                    Crc = DownloadManager.Current.CRC,
                    Path = DownloadManager.Current.Path,
                    CheckCrc = FileManager.GetCRC(DownloadManager.Current.Path)
                };
            }
            using (var fs = File.Create("updater.bin")) {
                cfg.WriteTo(fs);
            }
        }

        public static void Load() {
            if (!File.Exists("updater.bin"))
                return;
            using (var fs = File.OpenRead("updater.bin")) {
                var cfg = SystemConfig.Config.Parser.ParseFrom(fs);
                if (cfg.Win != null) {
                    Window.Width = cfg.Win.W;
                    Window.Height = cfg.Win.H;
                    if (cfg.Win.Maximized) {
                        Window.WindowState = WindowState.Maximized;
                    }
                }
                if (cfg.Download != null && cfg.Download.Crc != 0 && File.Exists(cfg.Download.Path)
                    && cfg.Download.CheckCrc == FileManager.GetCRC(cfg.Download.Path)) {
                    DownloadManager.Loaded = cfg.Download;
                }
            }
        }

        public static TrayIcon Tray = new TrayIcon();
        public static MainWindow Window;
        public static Client Tcp;
    }
}
