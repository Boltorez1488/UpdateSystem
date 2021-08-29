using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WebServer {
    public class MemoryMetrics {
        public double Total;
        public double Used;
        public double Free;
    }

    public class MemoryMetricsClient {
        public MemoryMetrics GetMetrics() {
            if (IsUnix()) {
                return GetUnixMetrics();
            }

            return GetWindowsMetrics();
        }

        private static bool IsUnix() {
            var isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            return isUnix;
        }

        private MemoryMetrics GetWindowsMetrics() {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info)) {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }

        public static long GetCurrentProcessMemory() {
            if (!IsUnix()) {
                return Process.GetCurrentProcess().PagedMemorySize64;
            }
            return 0;

            //var output = "";

            //var pid = Process.GetCurrentProcess().Id;
            
            //var cmd = $"pmap {pid} | tail -n 1 | awk '/[0-9]K/{{print $2}}'";
            //var info = new ProcessStartInfo {
            //    FileName = "/bin/bash",
            //    Arguments = $"-c \"{cmd}\"",
            //    RedirectStandardOutput = true,
            //    CreateNoWindow = true
            //};

            //using (var process = Process.Start(info)) {
            //    output = process.StandardOutput.ReadToEnd();
            //    //Console.WriteLine(output.Trim('\n', ' ', 'K'));
            //}

            //return long.Parse(output.Trim('\n', ' ', 'K')) * 1000;
        }

        private MemoryMetrics GetUnixMetrics() {
            var output = "";

            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info)) {
                output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = double.Parse(memory[1]);
            metrics.Used = double.Parse(memory[2]);
            metrics.Free = double.Parse(memory[3]);

            return metrics;
        }
    }
}
