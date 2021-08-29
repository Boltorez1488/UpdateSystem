using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebServer.Models;

namespace WebServer.Cron {
    public class CronManager {
        private static CancellationTokenSource tokenSrc;
        public static MemoryMetricsClient Mem = new MemoryMetricsClient();

        public static void Start() {
            CPUUsage.Start();
            tokenSrc = new CancellationTokenSource();
            SenderCPU();
            SenderOnline();
            PatchesCollector();
        }   
        
        public static void Stop() {
            CPUUsage.Stop();
            tokenSrc.Cancel();
        }

        public static void PatchesCollector() {
            Task.Run(async () => {
                await Task.Delay(60 * 1000);
                lock (Controllers.HomeController.Patches) {
                    var remove = new List<string>();
                    foreach (var p in Controllers.HomeController.Patches) {
                        if ((DateTime.UtcNow - p.Value.Last).TotalMinutes < 30)
                            continue;
                        remove.Add(p.Key);
                    }
                    foreach(var k in remove) {
                        Controllers.HomeController.Patches.Remove(k);
                    }
                }
                PatchesCollector();
            }, tokenSrc.Token);
        }

        public static void SendCPU(string clientId = null) {
            var mem = Mem.GetMetrics();
            var info = new InfoCPU { 
                Cores = Environment.ProcessorCount,
                ProcessMemory = MemoryMetricsClient.GetCurrentProcessMemory(),
                UsageCPU = CPUUsage.Usage,

                TotalMemory = mem.Total,
                UsedMemory = mem.Used,
                FreeMemory = mem.Free
            };
            if (clientId == null) {
                Startup.Hub.Clients.All.SendAsync("CPU", info);
            } else {
                Startup.Hub.Clients.Client(clientId).SendAsync("CPU", info);
            }
        }

        private static void SenderCPU() {
            Task.Run(async () => {
                await Task.Delay(1000);
                SendCPU();
                SenderCPU();
            }, tokenSrc.Token);
        }

        private static void SenderOnline() {
            Task.Run(async () => {
                await Task.Delay(1000);
                await Hubs.PanelHub.ReloadMetrics();
                SenderOnline();
            }, tokenSrc.Token);
        }
    }
}
