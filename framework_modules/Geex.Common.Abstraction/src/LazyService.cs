using System;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.Abstractions
{
    public class LazyService<T>
    {
        private readonly IServiceProvider _provider;
        public T? Value => _provider.GetService<T>();
        public LazyService(IServiceProvider provider)
        {
            _provider = provider;
        }
    }
}
