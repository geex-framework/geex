﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Migrations;
using Geex.Common.Abstraction.Storage;

using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;

using MediatX;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            var options = (GeexModuleOption<TModule>)(this.ServiceConfigurationContext.Services
              .GetSingletonInstanceOrNull(type) ?? Activator.CreateInstance(type));
            optionsAction.Invoke(options);
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
            context.Services.AddMediatR(x =>
            {
                x.RegisterServicesFromAssembly(typeof(TModule).Assembly);
                x.AutoRegisterRequestProcessors = true;
            });
            this.SchemaBuilder.AddModuleTypes(this.GetType());
            base.ConfigureServices(context);
        }
    }

    public class GeexModule : AbpModule
    {
        public IRequestExecutorBuilder SchemaBuilder => this.ServiceConfigurationContext.Services.GetSingletonInstance<IRequestExecutorBuilder>();
        public static HashSet<Assembly> KnownModuleAssembly { get; } = new HashSet<Assembly>();
        public static HashSet<Type> RootTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> Modules { get; } = new HashSet<Type>();
        public static HashSet<Type> ClassEnumTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> DirectiveTypes { get; } = new HashSet<Type>();
        public static HashSet<Type> ObjectTypes { get; } = new HashSet<Type>();
        public static Dictionary<Type, Type[]> RemoteNotificationHandlerTypes { get; } = new Dictionary<Type, Type[]>();
        public static HashSet<Type> RequestHandlerTypes { get; } = new HashSet<Type>();
    }

    public abstract class GeexEntryModule<T> : GeexModule<T> where T : GeexModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var env = context.Services.GetSingletonInstance<IWebHostEnvironment>();
            var coreModuleOptions = context.Services.GetSingletonInstanceOrNull<GeexCoreModuleOptions>();

            if (coreModuleOptions.RabbitMq != null)
            {
                var options = coreModuleOptions.RabbitMq;
                context.Services.AddMediatX();
                context.Services.AddMediatXRabbitMQ(x =>
                {
                    x.HostName = options.HostName;
                    x.Port = options.Port;
                    x.Password = options.Password;
                    x.Username = options.Username;
                    x.VirtualHost = options.VirtualHost;
                    x.Durable = true;
                    x.AutoDelete = false;
                    x.QueueName = coreModuleOptions.AppName;
                    x.DeDuplicationEnabled = true;
                    x.SerializerSettings = System.Text.Json.Json.InternalSerializeSettings;
                    x.NotificationHandlerTypes = RemoteNotificationHandlerTypes;
                });
            }

            context.Services.AddWebSockets(x => { });

            base.ConfigureServices(context);
            this.SchemaBuilder.EnsureGqlTypes();
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
            app.UseVoyager("/graphql", "/voyager");
        }

        /// <inheritdoc />
        public override async Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var coreModuleOptions = context.ServiceProvider.GetService<GeexCoreModuleOptions>();
            var app = context.GetApplicationBuilder();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapGraphQL().WithOptions(new GraphQLServerOptions()
                {
                    EnableSchemaRequests = !coreModuleOptions.DisableIntrospection,
                    EnforceMultipartRequestsPreflightHeader = false,
                    EnableGetRequests = false,
                    Tool =
                    {
                        Enable = !coreModuleOptions.DisableIntrospection,
                    }
                });
            });
            await base.OnPostApplicationInitializationAsync(context);
            if (coreModuleOptions.AutoMigration)
            {
                var migrations = context.ServiceProvider.GetServices<DbMigration>();
                var appliedMigrations = await DB.Find<Migration>().Project(x => x.Number).ExecuteAsync();

                var sortedMigrations = migrations.OrderBy(x => x.Number);

                foreach (var migration in sortedMigrations)
                {
                    if (!appliedMigrations.Contains(migration.Number))
                    {
                        using var scope = context.ServiceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().As<GeexDbContext>();
                        using var _ = dbContext.DisableAllDataFilters();
                        await dbContext.MigrateAsync(migration);
                    }
                }
            }
        }
    }
}
