using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Authorization;
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
