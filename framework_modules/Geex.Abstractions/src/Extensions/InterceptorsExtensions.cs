using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

namespace Geex
{
    public static class InterceptorsExtensions
    {
        public static IServiceCollection AddDataFilters(this IServiceCollection serviceCollection)
        {
            var dataFilterTypes = serviceCollection.Where(x => x.ServiceType.IsAssignableTo<IDataFilter>()).Select(x => x.ServiceType);
            foreach (var filterType in dataFilterTypes)
            {
                DbContext.StaticDataFilters.TryAdd(filterType.GenericTypeArguments[0], sp => sp.GetService(filterType).As<IDataFilter>());
            }
            return serviceCollection;
        }
    }
}
