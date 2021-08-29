using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using WebServer.Models;

namespace WebServer.Hubs {
    [Authorize]
    public class PanelHub : Hub {
        public static IHubContext<PanelHub> HubContext { get; private set; }

        public PanelHub(IHubContext<PanelHub> hubContext) {
            HubContext = hubContext;
        }

        public static async Task ReloadMetrics() {
            await Startup.Hub.Clients.All.SendAsync("OnlineChanged", Server.Session.Count);
            var downloads = Server.Session.Clients.Sum(x => x.Downloader.Downloads.Count);
            await Startup.Hub.Clients.All.SendAsync("DownloadsChanged", downloads);
        }

        public async Task Accept(string dir) {
            await Clients.Caller.SendAsync("DownloadState", Server.Managers.DownloadManager.IsLocked);
            await Clients.Caller.SendAsync("FilesReload", IsReloading);
            await Clients.Caller.SendAsync("ConnectionId", Context.ConnectionId);
            await ReloadMetrics();
            Cron.CronManager.SendCPU(Context.ConnectionId);

            FolderBrowser.SendFolder(Clients.Caller, dir);
        }

        public override Task OnConnectedAsync() {
            Clients.Caller.SendAsync("DownloadState", Server.Managers.DownloadManager.IsLocked);
            Clients.Caller.SendAsync("ConnectionId", Context.ConnectionId);
            Clients.Caller.SendAsync("FilesReload", IsReloading);
            _ = ReloadMetrics();
            return base.OnConnectedAsync();
        }

        public static bool IsReloading = false;
        private static bool isLocking = false;
        public async Task DownloadLock(bool state) {
            if (isLocking)
                return;

            isLocking = true;
            try {
                if (state) {
                    Server.Managers.DownloadManager.Lock();
                } else {
                    if (FolderBrowser.IsWaitApply) {
                        IsReloading = true;
                        await Clients.All.SendAsync("FilesReload", true);
                        Server.Managers.FileManager.Reload();
                        Server.Managers.FileManager.RemoverCheck();
                        IsReloading = false;
                        await Clients.All.SendAsync("FilesReload", false);
                        FolderBrowser.IsWaitApply = false;
                    }
                    Server.Managers.DownloadManager.Unlock();
                }
                isLocking = false;
            } catch(Exception) {
                isLocking = false;
                throw;
            }
            await Clients.All.SendAsync("DownloadState", Server.Managers.DownloadManager.IsLocked);
        }

        public Task SwitchFolder(string dir) {
            Models.FolderBrowser.SendFolder(Clients.Caller, dir);
            return Task.CompletedTask;
        }

        public Task CreateFolder(string dir, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            Models.FolderBrowser.CreateFolder(Clients.Caller, dir, root);
            return Task.CompletedTask;
        }

        public Task RemoveFolder(string dir, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            Models.FolderBrowser.RemoveFolder(Clients.Caller, dir, root);
            return Task.CompletedTask;
        }

        public Task RenameFolder(string dir, string newDir, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            Models.FolderBrowser.RenameFolder(Clients.Caller, dir, newDir, root);
            return Task.CompletedTask;
        }

        public Task RemoveFile(string fpath, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            Models.FolderBrowser.RemoveFile(Clients.Caller, fpath, root);
            return Task.CompletedTask;
        }

        public Task RenameFile(string fpath, string name, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            Models.FolderBrowser.RenameFile(Clients.Caller, fpath, name, root);
            return Task.CompletedTask;
        }

        public Task CreateRemover(string path, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            FolderBrowser.CreateRemover(Clients.Caller, path, root);
            return Task.CompletedTask;
        }

        public Task RemoveRemover(string path, string root) {
            if (!Server.Managers.DownloadManager.IsLocked)
                return Task.CompletedTask;
            FolderBrowser.RemoveRemover(Clients.Caller, path, root);
            return Task.CompletedTask;
        }
    }
}
