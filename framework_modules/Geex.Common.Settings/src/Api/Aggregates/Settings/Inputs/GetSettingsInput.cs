using System.Collections.Generic;
using System.Linq;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Settings.Abstraction;

using MediatR;

namespace Geex.Common.Settings.Api.Aggregates.Settings.Inputs
{
    public class GetSettingsInput : QueryInput<ISetting>
    {
        public SettingScopeEnumeration Scope { get; set; }
        public List<SettingDefinition> SettingDefinitions { get; set; }
        public string FilterByName { get; set; }


        public GetSettingsInput(SettingScopeEnumeration scope)
        {
            Scope = scope;
        }

        public GetSettingsInput(SettingScopeEnumeration scope, params SettingDefinition[] settingDefinitions)
        {
            Scope = scope;
            SettingDefinitions = settingDefinitions.ToList();
        }

        public GetSettingsInput(SettingScopeEnumeration scope, string filterByName)
        {
            Scope = scope;
            this.FilterByName = filterByName;
        }

    }
}