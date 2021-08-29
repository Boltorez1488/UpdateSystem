using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebServer.Models {
    public class InfoCPU {
        public int Cores { get; set; }
        public long ProcessMemory { get; set; }
        public double UsageCPU { get; set; }

        public double TotalMemory { get; set; }
        public double UsedMemory { get; set; }
        public double FreeMemory { get; set; }
    }
}
