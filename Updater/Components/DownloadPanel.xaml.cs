using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PropertyChanged;
using Updater.Managers;

namespace Updater.Components {
    [AddINotifyPropertyChangedInterface]
    public partial class DownloadPanel : UserControl {
        public int Progress => TotalSize == 0 ? 0 : (int)((double)Size / TotalSize * ProgressMax);
        public int ProgressMax => 100000;

        public int TotalProgress => TotalGlobalSize == 0 ? 0 : (int)((double)GlobalSize / TotalGlobalSize * TotalProgressMax);
        public int TotalProgressMax => 100000;

        public string PrintSize => $"{Tools.FormatSize(Size)}/{Tools.FormatSize(TotalSize)}";
        public string PrintTotalSize => $"{Tools.FormatSize(GlobalSize)}/{Tools.FormatSize(TotalGlobalSize)}";

        [AlsoNotifyFor("PrintSpeed")]
        public int Speed { get; set; }
        public string PrintSpeed => Speed == 0 ? "Остановлено" : $"{Tools.FormatSize(Speed)}/с";

        public string File { get; set; }

        [AlsoNotifyFor("PrintTotal")]
        public int Count { get; set; }
        [AlsoNotifyFor("PrintTotal")]
        public int TotalCount { get; set; }

        public string PrintTotal => $"{Count}/{TotalCount}";

        // Current file size
        [AlsoNotifyFor("Progress", "PrintSize")]
        public ulong Size { get; set; }
        [AlsoNotifyFor("Progress", "PrintSize")]
        public ulong TotalSize { get; set; }

        // All files size
        [AlsoNotifyFor("TotalProgress", "PrintTotalSize")]
        public ulong GlobalSize { get; set; }
        [AlsoNotifyFor("TotalProgress", "PrintTotalSize")]
        public ulong TotalGlobalSize { get; set; }

        [AlsoNotifyFor("IsPauseBtn", "IsPlayBtn")]
        public bool IsPaused { get; set; }
        public Visibility IsPauseBtn => !IsPaused ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPlayBtn => IsPaused ? Visibility.Visible : Visibility.Collapsed;

        public DownloadPanel() {
            InitializeComponent();
        }

        private void Play_Click(object sender, RoutedEventArgs e) {
            IsPaused = false;
            EntryPoint.Window.SetPaused(false);
            DownloadManager.PartCatcher();
        }

        private void Pause_Click(object sender, RoutedEventArgs e) {
            IsPaused = true;
            EntryPoint.Window.SetPaused(true);
            Speed = 0;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Task.Run(DownloadManager.OnCancel);
        }
    }
}
