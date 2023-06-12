using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Hosting
{
    public static class MicrosoftExtensionsHosting
    {
        public static bool IsUnitTest(this IWebHostEnvironment hostEnvironment)
        {

            return hostEnvironment.EnvironmentName == "UnitTest";
        }
    }
}
