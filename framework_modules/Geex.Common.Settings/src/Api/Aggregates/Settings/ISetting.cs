using Geex.Common.Settings.Abstraction;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Geex.Common.Abstraction.MultiTenant;
using MongoDB.Entities;

namespace Geex.Common.Settings.Api.Aggregates.Settings
{
    public interface ISetting : IEntityBase
    {
        SettingScopeEnumeration Scope { get; }
        string ScopedKey { get; }
        JsonNode Value { get; }
        SettingDefinition Name { get; }
    }
}
