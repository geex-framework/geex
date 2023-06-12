using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Interceptors
{
    public static class InterceptorsExtensions
    {
        /// <summary>
        /// Register interceptors
        /// </summary>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        public static IServiceCollection AddDataInterceptors(this IServiceCollection serviceCollection)
        {
            var interceptorTypes = serviceCollection.Where(x => x.ServiceType.IsAssignableTo<IDataInterceptor>()).Select(x => x.ServiceType);
            foreach (var interceptorType in interceptorTypes)
            {
                DbContext.StaticDataInterceptors.TryAdd(interceptorType.GenericTypeArguments[0], sp => sp.GetService(interceptorType).As<IDataInterceptor>());
            }
            return serviceCollection;
        }

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
