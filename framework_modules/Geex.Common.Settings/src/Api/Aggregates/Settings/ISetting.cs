using Geex.Common.Settings.Abstraction;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;
using Volo.Abp.DependencyInjection;
using System.IO;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Settings.Api.Aggregates.Settings
{
    public interface ISetting : IEntityBase
    {
        SettingScopeEnumeration Scope { get; }
        string? ScopedKey { get; }
        JsonNode? Value { get; }
        SettingDefinition Name { get; }
    }
}
