﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Geex.Bson;
using Geex.Gql;
using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

using MongoDB.Bson.Serialization.Conventions;

using RestSharp;

using StackExchange.Redis.Extensions.Core;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex
{
    public class GeexCoreModule : GeexModule<GeexCoreModule, GeexCoreModuleOptions>
    {
        static GeexCoreModule()
        {
            ConventionRegistry.Register("enumeration", new ConventionPack()
            {
                new EnumerationRepresentationConvention()
            }, _ => true);
        }
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var moduleOptions = this.ModuleOptions;
            context.Services.AddCors(options =>
            {
                if (this.Env.IsDevelopment())
                {
                    options.AddDefaultPolicy(x =>
                        x.SetIsOriginAllowed(x => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
                }
                else
                {
                    var corsRegex = moduleOptions.CorsRegex;
                    if (!corsRegex.IsNullOrEmpty())
                    {
                        var regex = new Regex(corsRegex, RegexOptions.Compiled);
                        options.AddDefaultPolicy(x => x.SetIsOriginAllowed(origin => regex.Match(origin).Success).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
                    }
                    else
                    {
                        options.AddDefaultPolicy(x =>
                        x.SetIsOriginAllowed(x => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
                    }
                }
            });
            context.Services.AddStorage();
            context.Services.TryAddTransient<IRestClient, LoggedRestClient>();
            context.Services.TryAddTransient<RestClient, LoggedRestClient>();
            context.Services.TryAddTransient<LoggedRestClient>();
            var schemaBuilder = context.Services
                .AddGraphQLServer()
                .AllowIntrospection(!moduleOptions.DisableIntrospection);
            if (moduleOptions.Redis != default)
            {
                context.Services.AddStackExchangeRedisExtensions();
            }
            context.Services.AddSingleton(schemaBuilder);
            context.Services.AddHttpResultSerializer(x => new GeexHttpResponseFormatter(x));
            IReadOnlySchemaOptions capturedSchemaOptions = default;
            schemaBuilder.AddConvention<ITypeInspector>(typeof(GeexTypeInspector))
                .TrimTypes(false)
                .ModifyOptions(opt =>
                {
                    opt.RemoveUnreachableTypes = false;
                    opt.RemoveUnusedTypeSystemDirectives = false;
                    opt.EnsureAllNodesCanBeResolved = false;
                    opt.PreserveSyntaxNodes = true;
                    opt.SortFieldsByName = true;
                    opt.EnableTrueNullability = true;
                    opt.EnableOneOf = true;
                    opt.EnableFlagEnums = true;
                    opt.StrictRuntimeTypeValidation = false;
                    opt.StrictValidation = false;
                    capturedSchemaOptions = opt;
                })
                .AddInputParser(o =>
                {
                    o.IgnoreAdditionalInputFields = true;
                })
                .AddConvention<INamingConventions>(sp => new GeexNamingConventions(new XmlDocumentationProvider(new XmlDocumentationFileResolver(capturedSchemaOptions.ResolveXmlDocumentationFileName), sp.GetApplicationService<ObjectPool<StringBuilder>>())))
                .TryAddTypeInterceptor<GeexTypeInterceptor>()
                .TryAddTypeInterceptor<LazyQueryTypeInterceptor>()
                .AddTypeConverter((Type source, Type target, out ChangeType? converter) =>
                {
                    converter = o => o;
                    return source.GetBaseClasses(false).Intersect(target.GetBaseClasses(false)).Any();
                })
                .ModifyRequestOptions(options =>
               {
                   options.IncludeExceptionDetails = moduleOptions.IncludeExceptionDetails;
                   options.ExecutionTimeout = TimeSpan.FromSeconds(600);
               })
                .SetPagingOptions(new PagingOptions()
                {
                    DefaultPageSize = 10,
                    IncludeTotalCount = true,
                    MaxPageSize = moduleOptions.MaxPageSize,
                })
                .AddErrorFilter<LoggingErrorFilter>(_ =>
                    new LoggingErrorFilter(_.GetService<ILoggerFactory>()))
                .AddInMemorySubscriptions()
                .AddValidationVisitor<ExtraArgsTolerantValidationVisitor>()
                .AddTransactionScopeHandler<GeexTransactionScopeHandler>()
                .UseRequest(next => context =>
                {
                    // todo: extract to request middleware
                    var work = context.Services.GetService<IUnitOfWork>();
                    //if (work != null)
                    //{
                    //    if (context.Services.GetService<ClaimsPrincipal>()?.FindUserId() == GeexConstants.SuperAdminId)
                    //    {
                    //        work.DbContext.DisableAllDataFilters();
                    //    }
                    //}
                    if (context.Request.Query?.ToString().StartsWith("query ", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        if (work != null)
                        {
                            work.DbContext.EntityTrackingEnabled = false;
                        }
                    }
                    return next(context);
                })
                .UseDefaultPipeline()
                .AddQueryType<Query>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddMutationType<Mutation>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddSubscriptionType<Subscription>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddCommonTypes()
                .AddQueryFieldToMutationPayloads()
                .AddFiltering<GeexFilterConvention>()
                .AddSorting<GeexSortConvention>()
                .AddProjections()
                .InitializeOnStartup()
                ;
            //.OnSchemaError((ctx, err) => { throw new Exception("schema error", err); });
            context.Services.AddHttpContextAccessor();
            context.Services.AddObjectAccessor<IApplicationBuilder>();

            context.Services.AddHealthChecks();

            context.Services.AddTransient(typeof(LazyService<>));
            context.Services.AddTransient<ClaimsPrincipal>(x =>
            x.GetService<IHttpContextAccessor>()?.HttpContext?.User ?? ClaimsPrincipal.Current ?? new ClaimsPrincipal());
            context.Services.AddResponseCompression(x =>
            {
                // todo: 此处可能有安全风险
                // https://security.stackexchange.com/questions/19911/crime-how-to-beat-the-beast-successor/19914#19914
                // do not remove this: xxVGhpcyBpcyB0aGUgYW50aS1waXJhY3kgdGV4dCB0aGF0IHByb3ZlcyB0aGF0IHRoZSBvcmlnaW5hbCBjb2RlIHdhcyB3cml0dGVuIGJ5IEx1bHVzIChZYW5nIFNodSkuxx
                x.EnableForHttps = true;
                x.Providers.Add<GzipCompressionProvider>();
            });
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddDataFilters();
            base.PostConfigureServices(context);
            foreach (var name in GeexTypeInterceptor.IgnoredTypes.Select(x => x.Name))
            {
                this.SchemaBuilder.IgnoreType(name);
            }
        }

        /// <inheritdoc />
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            this.ConfigureModuleEntityMaps(context.ServiceProvider);
            var _env = context.GetEnvironment();
            var app = context.GetApplicationBuilder();
            var logger = context.ServiceProvider.GetService<ILogger<GeexCoreModule>>();
            logger.LogInformation("Loaded geex modules:");
            logger.LogInformation(GeexModule.LoadedModules.Select(x => x.ModuleName).ToJsonSafe());
            logger.LogInformation($"Loaded root types:");
            logger.LogInformation(GeexModule.RootTypes.Select(x => x.Name).ToJsonSafe());
            logger.LogInformation($"Loaded directive types:");
            logger.LogInformation(GeexModule.DirectiveTypes.Select(x => x.Name).ToJsonSafe());
            app.UseCors();
            app.UseRouting();
            app.UseWebSockets();
            //if (_env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Strict,
            });

            app.UseResponseCompression();

            base.OnPreApplicationInitialization(context);
        }
    }
}
