using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer {
    public class CPUUsage {
        public static double Usage = 0;
        
        private static CancellationTokenSource tokenSrc;
        public static void Start() {
            tokenSrc = new CancellationTokenSource();
            Reload();
        }

        public static void Stop() {
            tokenSrc.Cancel();
        }

        private static void Reload() {
            Task.Run(async () => {
                Usage = await GetCpuUsageForProcess();
                await Task.Delay(500);
                Reload();
            }, tokenSrc.Token);
        }

        private static async Task<double> GetCpuUsageForProcess() {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }
    }
}
