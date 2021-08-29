using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;

namespace Server {
    public class SpamConfig {
        public int Seconds { get; set; } = 10;
        public int Allow { get; set; } = 1000;
    }

    public class ServerConfig {
        public string WebKey { get; set; } = "1234";
        public int WebPort { get; set; } = 5000;

        public int Port { get; set; } = 1111;
        public int AliveTimeout { get; set; } = 15;
        public int MaxPacketsReadPerTick { get; set; } = 100;

        public SpamConfig Spam { get; set; } = new();

        public string DownloadFolder { get; set; } = "downloads";

        public string WebUrl { get; set; } = "site.com";
    }

    public class Config {
        public static ServerConfig Srv = new();

        public static void Load() {
            var path = Path.GetFullPath("cfg/server.xml");
            if (!File.Exists(path))
                return;

            var cfg = new ConfigurationBuilder()
                .AddXmlFile(path)
                .Build();
            cfg.Bind(Srv);
        }
    }
}
