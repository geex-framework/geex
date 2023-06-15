using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Geex.Client
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "x_Proj_xClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRouting();
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            var staticFileOptions = new StaticFileOptions()
            {
                OnPrepareResponse = ctx =>
                {
                    // 配置/数据不予缓存
                    var fileExt = (ctx.File.Name.Split('.').LastOrDefault() ?? "");
                    if (fileExt == "jsonc" || fileExt == "json" || fileExt == "html")
                    {
                        ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
                        return;
                    }
                    const int durationInSeconds = 60 * 60 * 24 * 1;
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
        "public,max-age=" + durationInSeconds;
                },
                ServeUnknownFileTypes = true
            };
            app.UseSpaStaticFiles(staticFileOptions);


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "x_Proj_xClientApp";
            });
        }
    }
}
