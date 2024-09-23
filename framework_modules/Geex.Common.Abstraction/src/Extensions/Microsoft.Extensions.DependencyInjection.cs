using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Geex.Common;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;
using Geex.Common.Gql.Types;

using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

using StackExchange.Redis;

using Volo.Abp.Modularity;


// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class Extension
    {
        public static IServiceCollection AddStorage(this IServiceCollection builder)
        {
            var commonModuleOptions = builder.GetSingletonInstance<GeexCoreModuleOptions>();
            var mongoUrl = new MongoUrl(commonModuleOptions.ConnectionString) { };
            var mongoSettings = MongoClientSettings.FromUrl(mongoUrl);
            mongoSettings.LinqProvider = LinqProvider.V2;
            if (commonModuleOptions.EnableDataLogging)
            {
                mongoSettings.ClusterConfigurator = cb =>
                {
                    ILogger<IMongoDatabase> logger = default;
                    cb.Subscribe<CommandStartedEvent>(e =>
                    {
                        logger ??= builder.GetServiceProviderOrNull()?.GetService<ILogger<IMongoDatabase>>();
                        logger?.LogInformation($"MongoDbCommandStartedEvent: {e.CommandName} - {e.Command.ToJson()}");
                    });
                    cb.Subscribe<CommandSucceededEvent>(e =>
                    {
                        logger?.LogInformation($"MongoDbCommandStartedEvent: {e.CommandName} success in {e.Duration.TotalMilliseconds}ms. Result: {e.Reply.ToJson()}");
                    });
                };
            }
            mongoSettings.ApplicationName = commonModuleOptions.AppName;
            DB.InitAsync(mongoUrl.DatabaseName ?? commonModuleOptions.AppName, mongoSettings).Wait();
            // todo: 这里需要在以后取消直接的db注入
            builder.AddScoped<DbContext>(x => x.GetService<IRepository>() as DbContext);
            builder.AddScoped<IUnitOfWork>(x => new GeexDbContext(x));
            // 直接从当前uow提取
            builder.AddScoped<IRepository>(x => x.GetService<IUnitOfWork>());
            return builder;
        }

        public static IServiceCollection AddHttpResultSerializer<T>(
      this IServiceCollection services, Func<IServiceProvider, T> instance)
      where T : class, IHttpResponseFormatter
        {
            services.RemoveAll<IHttpResponseFormatter>();
            services.AddSingleton<IHttpResponseFormatter, T>(instance);
            return services;
        }
        public static object? GetSingletonInstanceOrNull(this IServiceCollection services, Type type) => services.FirstOrDefault<ServiceDescriptor>((Func<ServiceDescriptor, bool>)(d => d.ServiceType == type))?.ImplementationInstance;

        public static IRequestExecutorBuilder EnsureGqlTypes(this IRequestExecutorBuilder schemaBuilder)
        {
            var rootTypes = schemaBuilder.Services.Where(x => x.ServiceType == typeof(ObjectTypeExtension)).ToList();
            schemaBuilder.Services.RemoveAll<ObjectTypeExtension>();
            schemaBuilder.Services.Add(rootTypes.Where(x => x.ImplementationType != null).Select(x => new ServiceDescriptor(x.ImplementationType!, x.ImplementationType!, ServiceLifetime.Scoped)));
            foreach (var serviceDescriptor in rootTypes)
            {
                if (serviceDescriptor.ImplementationType != null)
                    schemaBuilder.AddTypeExtension(serviceDescriptor.ImplementationType);
            }

            foreach (var objectType in GeexModule.ObjectTypes)
            {
                schemaBuilder.AddType(objectType);
            }

            foreach (var directiveType in GeexModule.DirectiveTypes)
            {
                schemaBuilder.AddDirectiveType(directiveType);
            }

            var classEnumTypes = GeexModule.ClassEnumTypes;
            foreach (var classEnumType in classEnumTypes)
            {
                var typeName = classEnumType.Name;
                if ((classEnumType.GetClassEnumRealType().BaseType.GetProperty(nameof(Enumeration.DynamicValues)).GetValue(null) as IEnumerable<IEnumeration>).Any())
                {
                    var enumGqlType = typeof(EnumerationType<>).MakeGenericType(classEnumType);
                    schemaBuilder.AddType(enumGqlType);
                    schemaBuilder.BindRuntimeType(classEnumType, enumGqlType);
                    schemaBuilder.AddConvention(typeof(IFilterConvention), sp => new FilterConventionExtension(x =>
                    {
                        x.BindRuntimeType(classEnumType, typeof(ClassEnumOperationFilterInputType<>).MakeGenericType(classEnumType));
                    }));
                    schemaBuilder.AddConvention(typeof(ISortConvention), sp => new SortConventionExtension(x =>
                    {
                        x.BindRuntimeType(classEnumType, typeof(DefaultSortEnumType));
                    }));
                }
                else
                {
                    Console.WriteLine("Enumeration got no member: " + classEnumType.FullName);
                }
            }

            return schemaBuilder;
        }

        public static IRequestExecutorBuilder AddModuleTypes(this IRequestExecutorBuilder schemaBuilder, Type gqlModuleType)
        {
            if (GeexModule.KnownModuleAssembly.AddIfNotContains(gqlModuleType.Assembly))
            {
                var moduleName = gqlModuleType.Name;
                var dependedModuleTypes = gqlModuleType.GetCustomAttribute<DependsOnAttribute>()?.DependedTypes;
                if (dependedModuleTypes?.Any() == true)
                {
                    foreach (var dependedModuleType in dependedModuleTypes)
                    {
                        schemaBuilder.AddModuleTypes(dependedModuleType);
                    }
                }
                var exportedTypes = gqlModuleType.Assembly.GetExportedTypes();

                var rootTypes = exportedTypes.Where(x => x.IsAssignableTo<ObjectTypeExtension>() && !x.IsAbstract);
                foreach (var rootType in rootTypes)
                {
                    var baseClasses = rootType.GetBaseClasses(typeof(ObjectTypeExtension));
                    var type2replace = baseClasses.ElementAtOrDefault(baseClasses.Length - 1);
                    if (type2replace != null && type2replace.BaseType?.BaseType?.BaseType == typeof(ObjectTypeExtension))
                    {
                        schemaBuilder.Services.ReplaceAll(x => x.ServiceType == typeof(ObjectTypeExtension) && x.ImplementationType == type2replace, () => new ServiceDescriptor(typeof(ObjectTypeExtension), rootType, ServiceLifetime.Scoped));
                    }
                    else
                    {
                        schemaBuilder.Services.AddScoped(typeof(ObjectTypeExtension), rootType);
                    }
                }
                GeexModule.RootTypes.AddIfNotContains(rootTypes);

                var objectTypes = exportedTypes.Where(x => !x.IsAbstract && x.IsAssignableTo<IType>()).Where(x => !x.IsGenericType || (x.IsGenericType && x.GenericTypeArguments.Any())).ToList();
                GeexModule.ObjectTypes.AddIfNotContains(objectTypes);

                var remoteNotificationHandleTypes = exportedTypes.Where(x => !x.IsAbstract && x.ImplementsOrInherits(typeof(IRemoteNotificationHandler)));
                //var inheritanceDeclarations = notificationHandleTypes.SelectMany(x => x.GetInterfaces().Where(y => y.ImplementsOrInherits(typeof(INotificationHandler<>))));
                //var notificationTypes = inheritanceDeclarations.Select(x => x.GenericTypeArguments[0]).ToArray().ToList();
                var remoteNotificationHandlers = remoteNotificationHandleTypes
                    .Select(x => (notifications: x.GetInterfaces().Where(y => y.ImplementsOrInherits(typeof(IRemoteNotificationHandler<>))).Select(x => x.GenericTypeArguments[0]).ToArray(), handlerType: x))
                    .ToList();
                var dic = remoteNotificationHandlers.ToDictionary(x => x.handlerType, x => x.notifications);
                GeexModule.RemoteNotificationHandlerTypes.AddIfNotContains(dic);

                var requestHandlers = exportedTypes.Where(x => !x.IsAbstract && x.ImplementsOrInherits(typeof(IRequestHandler<>))).ToList();
                GeexModule.RequestHandlerTypes.AddIfNotContains(requestHandlers);

                var classEnumTypes = exportedTypes.Where(x => !x.IsAbstract && x.IsClassEnum() && x.Name != nameof(Enumeration)).ToList();
                GeexModule.ClassEnumTypes.AddIfNotContains(classEnumTypes);

                var directiveTypes = exportedTypes.Where(x => !x.IsAbstract && x.IsAssignableTo<DirectiveType>()).Where(x => !x.IsGenericType || (x.IsGenericType && x.GenericTypeArguments.Any())).ToList();
                GeexModule.DirectiveTypes.AddIfNotContains(directiveTypes);

                foreach (var socketInterceptor in exportedTypes.Where(x => x.IsAssignableTo<ISocketSessionInterceptor>()).ToList())
                {
                    schemaBuilder.ConfigureSchemaServices(s => s.TryAdd(ServiceDescriptor.Scoped(typeof(ISocketSessionInterceptor), socketInterceptor)));
                }

                foreach (var requestInterceptor in exportedTypes.Where(x => x.IsAssignableTo<IHttpRequestInterceptor>()).ToList())
                {
                    schemaBuilder.ConfigureSchemaServices(s => s.TryAdd(ServiceDescriptor.Scoped(typeof(IHttpRequestInterceptor), requestInterceptor)));
                }
            }
            return schemaBuilder;
        }

        public static bool IsValidEmail(this string str)
        {
            return new Regex(@"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}").IsMatch(str);
        }

        public static bool IsValidPhoneNumber(this string str)
        {
            return new Regex(@"\d{11}").IsMatch(str);
        }



        public static bool IsClassEnum(this Type type)
        {
            if (type.IsValueType)
            {
                return false;
            }

            return type.IsAssignableTo<IEnumeration>();
        }

        public static IEnumerable<T> GetSingletonInstancesOrNull<T>(this IServiceCollection services) => services.Where<ServiceDescriptor>((Func<ServiceDescriptor, bool>)(d => d.ServiceType == typeof(T)))?.Select(x => x.ImplementationInstance).Cast<T>();

        public static IEnumerable<T> GetSingletonInstances<T>(this IServiceCollection services) => services.GetSingletonInstancesOrNull<T>() ?? throw new InvalidOperationException("Could not find singleton service: " + typeof(T).AssemblyQualifiedName);

        public static IServiceCollection ReplaceAll(
      this IServiceCollection collection,
      ServiceDescriptor descriptor)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            var serviceDescriptors = collection.Where((s => s.ServiceType == descriptor.ServiceType)).ToList();
            if (serviceDescriptors.Any())
                collection.RemoveAll(serviceDescriptors);
            collection.Add(descriptor);
            return collection;
        }

        public static IServiceCollection ReplaceAll(
      this IServiceCollection collection,
      Func<ServiceDescriptor, bool> predicate,
      Func<ServiceDescriptor> itemFactory)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            var serviceDescriptors = collection.Where(predicate).ToList();
            if (serviceDescriptors.Any())
                collection.RemoveAll(serviceDescriptors);
            collection.Add(itemFactory());
            return collection;
        }
    }
}
