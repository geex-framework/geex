using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Abstractions
{
    public class ServiceLocator
    {
        public static IServiceProvider Scoped
        {
            get
            {
                var provider = Global.GetService<IHttpContextAccessor>()?.HttpContext?.RequestServices;
                if (provider == default)
                {
                    throw new NotImplementedException("only http request scope is implemented.");
                }
                return provider;
            }
        }

        public static IServiceProvider Global { get; internal set; }
    }
}
