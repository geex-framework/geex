using System.Collections.Generic;
using System.Linq;
using Geex.Common.Requests;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;

namespace Geex.Common.Requests.Settings
{
    public record GetSettingsRequest : QueryRequest<ISetting>
    {
        public SettingScopeEnumeration? Scope { get; set; }
        public List<SettingDefinition>? SettingDefinitions { get; set; }
        public string? FilterByName { get; set; }


        public GetSettingsRequest(SettingScopeEnumeration scope)
        {
            Scope = scope;
        }

        public GetSettingsRequest(SettingScopeEnumeration scope, params SettingDefinition[] settingDefinitions)
        {
            Scope = scope;
            SettingDefinitions = settingDefinitions.ToList();
        }

        public GetSettingsRequest(SettingScopeEnumeration scope, string filterByName)
        {
            Scope = scope;
            FilterByName = filterByName;
        }

    }
}