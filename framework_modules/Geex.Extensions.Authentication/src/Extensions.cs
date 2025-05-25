using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Authentication;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
    public static class Extensions
    {

        public static ICurrentUser? GetCurrentUser(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<ICurrentUser>();
        }
    }
}
