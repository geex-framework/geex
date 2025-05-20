using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Settings;
using Geex.Common.BlobStorage.Api.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.BlobStorage.Core.Extensions
{
    public static class Extension
    {

        public static IBlobService? GetBlobService(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<IBlobService>();
        }
    }
}
