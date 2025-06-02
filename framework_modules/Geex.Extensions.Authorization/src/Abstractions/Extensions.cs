using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.Abstractions;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using NetCasbin.Abstractions;

namespace Geex.Extensions.Authorization
{
    public static class Extensions
    {
        public static IRbacEnforcer? GetEnforcer(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<IRbacEnforcer>();
        }
    }
}
