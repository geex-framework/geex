using System;
using System.Diagnostics.CodeAnalysis;
using Geex.Common.Authorization;

namespace Geex.Common.Settings.Api
{
    public class SettingsPermission : AppPermission<SettingsPermission>
    {
        public SettingsPermission([NotNull] string value) : base($"{typeof(SettingsPermission).DomainName()}.{value}")
        {
        }
        // 取消setting的权限控制, 只限制登录即可
        //public static SettingsPermission Query { get; } = new SettingsPermission("query.settings");
        public static SettingsPermission Edit { get; } = new SettingsPermission("mutation.editSetting");
    }
}
