using Geex.Common.Abstractions;

namespace Geex.Common.Settings.Aggregates
{
    public class SettingScopeEnumeration : Enumeration<SettingScopeEnumeration>
    {
        public int Priority { get; set; }
        public SettingScopeEnumeration(string name, string value, int priority) : base(name, value)
        {
            this.Priority = priority;
        }
        /// <summary>
        /// 全局运行时, 动态
        /// </summary>
        public static SettingScopeEnumeration Global { get; } = new(nameof(Global), nameof(Global), 2);
        /// <summary>
        /// 租户级, 动态
        /// </summary>
        public static SettingScopeEnumeration Tenant { get; } = new(nameof(Tenant), nameof(Tenant), 1);
        /// <summary>
        /// 用户级, 动态
        /// </summary>
        public static SettingScopeEnumeration User { get; } = new(nameof(User), nameof(User), 0);
    }
}
