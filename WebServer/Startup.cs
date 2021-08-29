using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

namespace WebServer {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            //services.AddDataProtection()
            //    .PersistKeysToFileSystem(new DirectoryInfo("keys"))
            //     .ProtectKeysWithCertificate(new X509Certificate2("openssl.crt", "1234"))
            //    .SetApplicationName("WebServer");

            services.AddSignalR();
            services.AddControllersWithViews();
            //services.AddAuthorization();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => {
                options.Cookie.Name = "Access";
                options.LoginPath = new PathString("/");
            });
            services.AddAntiforgery(o => o.Cookie.Name = "XSRF-TOKEN");
            services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
#if DEBUG
            services.AddRazorPages().AddRazorRuntimeCompilation();
#endif
            //if (env.IsDevelopment()) {
            //    services.AddRazorPages().AddRazorRuntimeCompilation();
            //}

            services.AddDataProtection()
               .SetApplicationName("WebServer")
               .PersistKeysToFileSystem(new DirectoryInfo("keys"))
               .SetDefaultKeyLifetime(TimeSpan.FromDays(14));
        }

        public static IHubContext<Hubs.PanelHub> Hub;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHubContext<Hubs.PanelHub> context) {
            Hub = context;

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            //app.UseCors("CorsPolicy");

            //app.UseAuthorization();

            //app.UseEndpoints(endpoints => {
            //    endpoints.MapControllerRoute(
            //        name: "default",
            //        pattern: "{controller=Home}/{action=Index}/{id?}");
            //});
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHub<Hubs.PanelHub>("/panel");
            });

            //Server.Session.OnClientConnect += c => {
            //    context.Clients.All.SendAsync("OnlineChanged", Server.Session.Count);
            //};
            //Server.Session.OnClientDisconnect += c => {
            //    context.Clients.All.SendAsync("OnlineChanged", Server.Session.Count);
            //};

            Cron.CronManager.Start();
        }
    }
}
