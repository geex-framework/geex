using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.Requests;

namespace Geex.Extensions.Settings.Requests
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
