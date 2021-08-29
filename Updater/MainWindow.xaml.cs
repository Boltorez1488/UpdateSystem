using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using MessageBox = System.Windows.Forms.MessageBox;
using Updater.Managers;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using PeanutButter.TrayIcon;
using PropertyChanged;

namespace Updater {
    public enum Mode {
        CanPlay,
        CanDownload,
        Download
    }

    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window {
        bool canClose = false;
        bool alreadyNotified = false;

        public void SetPaused(bool isPaused) {
            Dispatcher.Invoke(() => {
                if (isPaused) {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                } else {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            });
        }

        public void SetProgress(double progress) {
            Dispatcher.Invoke(() => {
                TaskbarItemInfo.ProgressValue = progress;
            });
        }

        public void SetFinished() {
            Dispatcher.Invoke(() => {
                TaskbarItemInfo.ProgressValue = 0;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            });
        }

        public void SetStarted() {
            Dispatcher.Invoke(() => {
                TaskbarItemInfo.ProgressValue = 0;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            });
        }

        private void InitIcon() {
            var icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico")).Stream);
            EntryPoint.Tray.Init(icon);

            EntryPoint.Tray.NotifyIcon.Text = Title;

            EntryPoint.Tray.AddMenuItem("Показать", Show);

            EntryPoint.Tray.AddMenuItem("Выход", () => {
                canClose = true;
                Close();
            });

            EntryPoint.Tray.AddMouseClickHandler(MouseClicks.Double, MouseButtons.Left, Show);

            EntryPoint.Tray.Show();
        }

        public MainWindow() {
            EntryPoint.Window = this;
            InitializeComponent();
            InitIcon();
            EntryPoint.Tcp.Start();
            EntryPoint.Load();
            if (!File.Exists("updater.bin")) {
                AddLineAsync("Информация: Это центр обновления файлов, он работает в реальном времени, все изменения на сервере сообщаются клиенту. Если хотите получать обновления сразу, не отключайте его");
            }
            TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();
        }

        public void FullClose() {
            canClose = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!canClose) {
                e.Cancel = true;
                Hide();
                if (!alreadyNotified) {
                    alreadyNotified = true;
                    EntryPoint.Tray.ShowBalloonTipFor(2000, "Напоминание", "Менеджер свернут в трей", ToolTipIcon.Info, () => {
                        EntryPoint.Window.Show();
                    });
                }
            } else {
                EntryPoint.Tcp.Socket.Close();
                EntryPoint.Save();
            }
        }

        public async void AddLineAsync(string line) {
            await Dispatcher.InvokeAsync(() => {
                if (log.Text.Length > 30000) {
                    log.Text = "";
                }
                var dt = DateTime.Now.ToString("dd.MM.yy HH:mm:ss");
                log.AppendText($"[{dt}]: " + line + "\n");
                log.ScrollToEnd();
            });
        }

        private Visibility Visibler(bool value) {
            return value ? Visibility.Visible : Visibility.Collapsed;
        }

        //====================================[ENGINE]====================================

        [AlsoNotifyFor("PrintConnected", "ConnectedColor")]
        public bool IsConnected { get; set; }

        public string PrintConnected => IsConnected ? "Подключен к серверу" : "Нет связи с сервером";
        public Brush ConnectedColor => IsConnected ? Brushes.LimeGreen : Brushes.Red;

        [AlsoNotifyFor("ErrorVisible")]
        public string LastError { get; set; }
        public Visibility ErrorVisible => Visibler(!IsConnected && !string.IsNullOrEmpty(LastError));

        private void Error_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(LastError, "Ошибка связи", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        [AlsoNotifyFor("DownloadVisible", "PlayVisible", "DownloadBtnVisible", "DownloadText")]
        public Mode CurMode { get; set; }

        public Visibility DownloadVisible => Visibler(CurMode == Mode.Download);
        public Visibility PlayVisible => Visibler(CurMode == Mode.CanPlay);
        public Visibility DownloadBtnVisible => Visibler(CurMode == Mode.CanDownload);

        public bool IsEnabledDownload { get; set; } = true;

        public string DownloadText => $"Доступно обновление ({Tools.FormatSize(FileManager.BrokenSize)})";

        private void Play_Click(object sender, RoutedEventArgs e) {
            if (!File.Exists("Bin/Game.exe")) {
                return;
            }
            Process.Start(new ProcessStartInfo {
                FileName = "Game.exe",
                WorkingDirectory = "Bin"
            });
            Close();
        }

        public static string NormalizePath(string path) {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        private void Download_Click(object sender, RoutedEventArgs e) {
            var procs = Process.GetProcessesByName("Game");
            if (procs.Length != 0) {
                var curPath = NormalizePath(Path.GetFullPath("Bin/Game.exe"));
                foreach(var proc in procs) {
                    if (NormalizePath(proc.MainModule.FileName) == curPath) {
                        var result = MessageBox.Show(
                            "Обнаружен запущенный экземпляр игры.\nЗакройте для продолжения.\nЗакрыть?",
                            "Предупреждение обновления",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                        if (result == System.Windows.Forms.DialogResult.Yes) {
                            if (!proc.CloseMainWindow())
                                proc.Kill();
                        }
                        return;
                    }
                }
            }
            Parallel.ForEach(FileManager.Remover, (path) => {
                try {
                    if (File.Exists(path)) {
                        File.Delete(path);
                        AddLineAsync($"Файл удалён \"{path}\"");
                    } else if (Directory.Exists(path)) {
                        Directory.Delete(path);
                        AddLineAsync($"Папка удалена \"{path}\"");
                    }
                } catch (Exception) {
                    //...
                }
            });
            Task.Run(() => {
                DownloadManager.StartDownload(FileManager.Broken.First());
            });
        }
    }
}
