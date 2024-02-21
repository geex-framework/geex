using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Autofac.Core;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Gql;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Gql;
using Geex.Common.Gql.Types;

using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Server;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;
using HotChocolate.Validation;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

using StackExchange.Redis.Extensions.Core;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common
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
            var schemaBuilder = context.Services
                .AddGraphQLServer()
                .AllowIntrospection(!moduleOptions.DisableIntrospection);
            if (moduleOptions.Redis != default)
            {
                context.Services.AddStackExchangeRedisExtensions();
            }
            context.Services.AddSingleton(schemaBuilder);
            context.Services.AddHttpResultSerializer(x => new GeexResultSerializerWithCustomStatusCodes(new LazyService<ClaimsPrincipal>(x)));
            IReadOnlySchemaOptions capturedSchemaOptions = default;
            schemaBuilder.AddConvention<ITypeInspector>(typeof(GeexTypeInspector))
                .ModifyOptions(opt =>
                {
                    opt.EnableOneOf = true;
                    capturedSchemaOptions = opt;
                })
                .AddConvention<INamingConventions>(sp => new GeexNamingConventions(new XmlDocumentationProvider(new XmlDocumentationFileResolver(capturedSchemaOptions.ResolveXmlDocumentationFileName), sp.GetApplicationService<ObjectPool<StringBuilder>>())))
                .TryAddTypeInterceptor<GeexTypeInterceptor>()
                .AddTypeConverter((Type source, Type target, out ChangeType? converter) =>
                {
                    converter = o => o;
                    return source.GetBaseClasses(false).Intersect(target.GetBaseClasses(false)).Any();
                })
                .ModifyRequestOptions(options =>
               {
                   options.IncludeExceptionDetails = moduleOptions.IncludeExceptionDetails;
                   options.ExecutionTimeout = TimeSpan.FromSeconds(300);
               })
                .SetPagingOptions(new PagingOptions()
                {
                    DefaultPageSize = 10,
                    IncludeTotalCount = true,
                    MaxPageSize = moduleOptions.MaxPageSize
                })
                .AddErrorFilter<LoggingErrorFilter>(_ =>
                    new LoggingErrorFilter(_.GetService<ILoggerFactory>()))
                .AddInMemorySubscriptions()
                .AddValidationVisitor<ExtraArgsTolerantValidationVisitor>()
                .AddTransactionScopeHandler<GeexTransactionScopeHandler>()
                .AddFiltering()
                .AddConvention<IFilterConvention>(new FilterConventionExtension(x => x.Provider(new GeexQueryablePostFilterProvider(y => y.AddDefaultFieldHandlers()))))
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddMutationType<Mutation>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddSubscriptionType<Subscription>(x => x.Field("_").Type<StringType>().Resolve(x => null))
                .AddCommonTypes()
                //.AddMutationConventions(new MutationConventionOptions()
                //{
                //    ApplyToAllMutations = true,
                //    InputTypeNamePattern = "{MutationName}Request",
                //    InputArgumentName = "request",
                //})
                .AddQueryFieldToMutationPayloads()
                .InitializeOnStartup()
                ;
            //.OnSchemaError((ctx, err) => { throw new Exception("schema error", err); });
            context.Services.AddHttpContextAccessor();
            context.Services.AddObjectAccessor<IApplicationBuilder>();

            context.Services.AddHealthChecks();

            context.Services.AddTransient(typeof(LazyService<>));
            context.Services.AddTransient<ClaimsPrincipal>(x =>
            x.GetService<IHttpContextAccessor>()?.HttpContext?.User);
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
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            this.ConfigureModuleEntityMaps(context.ServiceProvider);
            var _env = context.GetEnvironment();
            if (!_env.IsUnitTest())
            {
                var app = context.GetApplicationBuilder();

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
            }

            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}
