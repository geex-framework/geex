using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstractions;
using Geex.Common.Accounting;
using Geex.Common.Authorization;
using Geex.Common.BackgroundJob;
using Geex.Common.BlobStorage.Core;
using Geex.Common.Gql;
using Geex.Common.Gql.Types;
using Geex.Common.Identity.Core;
using Geex.Common.Logging;
using Geex.Common.Messaging.Api;
using Geex.Common.Messaging.Core;
using Geex.Common.Settings;

using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;

using StackExchange.Redis.Extensions.Core;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common
{
    [DependsOn(
        typeof(GeexCoreModule),
        typeof(AccountingModule),
        typeof(IdentityCoreModule),
        typeof(LoggingModule),
        typeof(MessagingCoreModule),
        typeof(BlobStorageCoreModule),
        typeof(BackgroundJobModule),
        typeof(SettingsModule),
        typeof(AuthorizationModule)
        )]
    public class GeexCommonModule : GeexModule<GeexCommonModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }
    }
}
