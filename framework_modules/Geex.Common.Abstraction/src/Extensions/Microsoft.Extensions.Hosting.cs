using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Hosting
{
    public static class MicrosoftExtensionsHosting
    {
        public static bool IsUnitTest(this IWebHostEnvironment hostEnvironment)
        {

            return hostEnvironment.EnvironmentName == "UnitTest";
        }

        public static IHost ConfigServiceLocator(this IHost host)
        {
            ServiceLocator.Global = host.Services;
            return host;
        }
    }
}
