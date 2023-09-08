using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Storage;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Bson.Serialization;
using MongoDB.Entities;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.Abstractions
{
    public abstract class GeexModule<TModule, TModuleOptions> : GeexModule<TModule> where TModule : GeexModule where TModuleOptions : GeexModuleOption<TModule>
    {
        private TModuleOptions _moduleOptions;
        protected new TModuleOptions ModuleOptions => this._moduleOptions ??= this.ServiceConfigurationContext.Services.GetSingletonInstance<TModuleOptions>();
    }
    public abstract class GeexModule<TModule> : GeexModule where TModule : GeexModule
    {
        public IConfiguration Configuration { get; private set; }
        public IWebHostEnvironment Env { get; private set; }

        public virtual void ConfigureModuleOptions(Action<GeexModuleOption<TModule>> optionsAction)
        {
            var type = this.GetType().Assembly.ExportedTypes.FirstOrDefault(x => x.IsAssignableTo<GeexModuleOption<TModule>>());
            if (type == default)
            {
                throw new InvalidOperationException($"{nameof(GeexModuleOption<TModule>)} of {nameof(TModule)} is not declared, cannot be configured.");
            }
            var options = (GeexModuleOption<TModule>?)this.ServiceConfigurationContext.Services.GetSingletonInstanceOrNull(type);
            optionsAction.Invoke(options!);
            this.ServiceConfigurationContext.Services.TryAdd(new ServiceDescriptor(type, options));
        }
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            Configuration = context.Services.GetConfiguration();
            Env = context.Services.GetSingletonInstanceOrNull<IWebHostEnvironment>();
            context.Services.Add(new ServiceDescriptor(typeof(GeexModule), this));
            context.Services.Add(new ServiceDescriptor(this.GetType(), this));
            this.InitModuleOptions();
            base.PreConfigureServices(context);
        }

        private void InitModuleOptions()
        {
            var type = this.GetType().Assembly.ExportedTypes.FirstOrDefault(x => x.IsAssignableTo<GeexModuleOption<TModule>>());
            if (type == default)
            {
                return;
            }
            var options = Activator.CreateInstance(type) as GeexModuleOption<TModule>;
            Configuration.GetSection(type.Name).Bind(options);
            this.ServiceConfigurationContext.Services.TryAdd(new ServiceDescriptor(type, options));
            this.ServiceConfigurationContext.Services.TryAdd(new ServiceDescriptor(typeof(GeexModuleOption<TModule>), options));
            //this.ServiceConfigurationContext.Services.GetRequiredServiceLazy<ILogger<GeexModule>>().Value.LogInformation($"Module loaded with options:{Environment.NewLine}{options.ToJson()}");
        }

        public virtual void ConfigureModuleEntityMaps(IServiceProvider serviceProvider)
        {
            foreach (var entityMapConfig in serviceProvider.GetServices<IBsonConfig>())
            {
                entityMapConfig.Map();
            }
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            this.SchemaBuilder.AddModuleTypes(this.GetType());
            context.Services.AddMediatR(configuration: configuration =>
            {
            }, typeof(TModule));
            base.ConfigureServices(context);
        }
    }

    public class GeexModule : AbpModule
    {
        public IRequestExecutorBuilder SchemaBuilder => this.ServiceConfigurationContext.Services.GetSingletonInstance<IRequestExecutorBuilder>();
        public static HashSet<Assembly> KnownModuleAssembly { get; } = new HashSet<Assembly>();
        public static HashSet<Type> RootTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> ClassEnumTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> DirectiveTypes { get; } = new HashSet<Type>();
    }

    public abstract class GeexEntryModule<T> : GeexModule<T> where T : GeexModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var env = context.Services.GetSingletonInstance<IWebHostEnvironment>();
            context.Services.AddWebSockets(x => { });

            base.ConfigureServices(context);
            this.SchemaBuilder.ConfigExtensionTypes();
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            base.PostConfigureServices(context);
        }

        public override async Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            //var _env = context.GetEnvironment();
            //var _configuration = context.GetConfiguration();
            var coreModuleOptions = context.ServiceProvider.GetService<GeexCoreModuleOptions>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health-check");
                endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions()
                {
                    EnableSchemaRequests = !coreModuleOptions.DisableIntrospection,
                    EnableGetRequests = false,
                    Tool =
                    {
                        Enable = !coreModuleOptions.DisableIntrospection,
                    }
                });
            });

            app.UseVoyager("/graphql", "/voyager");

            if (coreModuleOptions.AutoMigration)
            {
                new GeexDbContext(context.ServiceProvider, transactional:true).MigrateAsync().Wait();
            }
        }
    }
}
