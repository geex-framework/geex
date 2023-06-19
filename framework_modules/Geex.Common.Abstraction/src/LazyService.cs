using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Utilities;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp.DependencyInjection;

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
