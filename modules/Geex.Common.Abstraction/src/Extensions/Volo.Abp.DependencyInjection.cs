// Decompiled with JetBrains decompiler
// Type: Volo.Abp.ApplicationInitializationContextExtensions
// Assembly: Volo.Abp.AspNetCore, Version=4.2.2.0, Culture=neutral, PublicKeyToken=null
// MVID: A8D8584E-6B9F-411C-8F87-528860B491D1
// Assembly location: C:\Users\lulus\.nuget\packages\volo.abp.aspnetcore\4.2.2\lib\net5.0\Volo.Abp.AspNetCore.dll

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;

// ReSharper disable once CheckNamespace
namespace Volo.Abp.DependencyInjection
{
    public static class ApplicationInitializationContextExtensions
    {
        public static async Task InitializeApplicationAsync(this IApplicationBuilder app)
        {
            Check.NotNull<IApplicationBuilder>(app, nameof(app));
            app.ApplicationServices.GetRequiredService<ObjectAccessor<IApplicationBuilder>>().Value = app;
            IAbpApplicationWithExternalServiceProvider application = app.ApplicationServices.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
            IHostApplicationLifetime requiredService = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            requiredService.ApplicationStopping.Register((Action)(() => application.Shutdown()));
            requiredService.ApplicationStopped.Register((Action)(() => application.Dispose()));
            await application.InitializeAsync(app.ApplicationServices);
        }
        public static IApplicationBuilder GetApplicationBuilder(
          this ApplicationInitializationContext context)
        {
            return context.ServiceProvider.GetRequiredService<IObjectAccessor<IApplicationBuilder>>().Value;
        }

        public static IWebHostEnvironment GetEnvironment(
          this ApplicationInitializationContext context)
        {
            return context.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        }

        public static IConfiguration GetConfiguration(
          this ApplicationInitializationContext context)
        {
            return context.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        public static ILoggerFactory GetLoggerFactory(
          this ApplicationInitializationContext context)
        {
            return context.ServiceProvider.GetRequiredService<ILoggerFactory>();
        }
    }
}
