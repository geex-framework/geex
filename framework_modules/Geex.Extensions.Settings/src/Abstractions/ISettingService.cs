using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Geex.Extensions.Settings
{
    /// <summary>
    /// Service for managing application settings
    /// </summary>
    public interface ISettingService
    {
        /// <summary>
        /// Sets a setting value
        /// </summary>
        /// <param name="settingDefinition">Setting definition</param>
        /// <param name="scope">Setting scope</param>
        /// <param name="scopedKey">Scoped key (tenant code or user id)</param>
        /// <param name="value">Setting value</param>
        /// <returns>Setting entity</returns>
        Task<ISetting> SetAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey, JsonNode? value);

        /// <summary>
        /// Gets all active settings (user, tenant, and global)
        /// </summary>
        /// <returns>Collection of setting entities</returns>
        Task<List<ISetting>> GetActiveSettings();

        /// <summary>
        /// Gets all global settings
        /// </summary>
        /// <returns>List of global settings</returns>
        Task<List<ISetting>> GetGlobalSettings();

        /// <summary>
        /// Gets all tenant settings for current tenant
        /// </summary>
        /// <returns>List of tenant settings</returns>
        Task<List<ISetting>> GetTenantSettings();

        /// <summary>
        /// Gets all user settings for current user
        /// </summary>
        /// <returns>List of user settings</returns>
        Task<List<ISetting>> GetUserSettings();

        /// <summary>
        /// Gets a specific setting or null if not found
        /// </summary>
        /// <param name="settingDefinition">Setting definition</param>
        /// <param name="settingScope">Setting scope</param>
        /// <param name="scopedKey">Scoped key</param>
        /// <returns>Setting entity or null</returns>
        Task<ISetting?> GetOrNullAsync(SettingDefinition settingDefinition, SettingScopeEnumeration settingScope = default, string? scopedKey = default);

        /// <summary>
        /// Gets all available setting definitions
        /// </summary>
        IReadOnlyList<SettingDefinition> SettingDefinitions { get; }
    }
}
