using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionDecorateExtensions
{
    public static IServiceCollection DecorateRegistered<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService, TService> decorator)
        where TService : class
    {
        var descriptors = services.Where(x => x.ServiceType == typeof(TService)).ToList();
        if (descriptors.Count == 0)
        {
            return services;
        }

        foreach (var descriptor in descriptors)
        {
            var index = services.IndexOf(descriptor);
            services.RemoveAt(index);
            services.Insert(index, ServiceDescriptor.Describe(
                typeof(TService),
                sp => decorator(sp, CreateInstance<TService>(sp, descriptor)),
                descriptor.Lifetime));
        }

        return services;
    }

    private static TService CreateInstance<TService>(IServiceProvider sp, ServiceDescriptor descriptor)
        where TService : class
    {
        if (descriptor.ImplementationInstance is TService instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (TService)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is null)
        {
            throw new InvalidOperationException($"Cannot decorate service {typeof(TService).Name} without an implementation.");
        }

        return (TService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
    }
}
