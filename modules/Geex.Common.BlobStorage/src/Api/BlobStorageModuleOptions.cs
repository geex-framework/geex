using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using HotChocolate.Execution.Options;

namespace Geex.Common.BlobStorage.Api
{
    public class BlobStorageModuleOptions : IGeexModuleOption<BlobStorageApiModule>
    {
        public string FileDownloadPath { get; set; } = "/download";
    }
}
