using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace WebServer {
    public class Program {
        public static void Main(string[] args) {
            Server.Program.Start();
            CreateHostBuilder(args).Build().Run();
            Cron.CronManager.Stop();
            Server.Program.Stop();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseKestrel(options => {
                        // Default port
                        options.ListenAnyIP(Server.Config.Srv.WebPort);
                        options.Limits.MaxRequestBodySize = 4096L * 1024 * 1024;

                        // Hub bound to TCP end point
                        //options.Listen(IPAddress.Any, 9001, builder => {
                        //    builder.UseHub<Hubs.ChatHub>();
                        //});
                    });
                    //webBuilder.UseIISIntegration();
                    webBuilder.UseStartup<Startup>();
                });
    }
}
