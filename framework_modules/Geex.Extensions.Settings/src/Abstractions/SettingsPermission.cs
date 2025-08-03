using System.Diagnostics.CodeAnalysis;


namespace Geex.Extensions.Settings
{
    public class SettingsPermission : AppPermission<SettingsPermission>
    {
        public const string Prefix = "settings";
        public SettingsPermission([NotNull] string value) : base($"{Prefix}_{value}")
        {
        }
        // 取消setting的权限控制, 只限制登录即可
        //public static SettingsPermission Query { get; } = new SettingsPermission("query_settings");
        public static SettingsPermission Edit { get; } = new SettingsPermission("mutation_editSetting");
    }
}
