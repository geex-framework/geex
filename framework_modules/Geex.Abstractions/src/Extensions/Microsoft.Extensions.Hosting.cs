using System;
using Geex;
using Geex.Abstractions;
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

        [Obsolete("ServiceLocator will be automatically registered.")]
        public static IHost ConfigServiceLocator(this IHost host)
        {
            ServiceLocator.Global = host.Services;
            return host;
        }
    }
}
