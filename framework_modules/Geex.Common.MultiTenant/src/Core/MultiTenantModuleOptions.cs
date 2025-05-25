using Geex.Abstractions;

namespace Geex.Common.MultiTenant.Core
{
    public class MultiTenantModuleOptions : GeexModuleOption<MultiTenantModule>
    {
        /// <summary>
        /// !未实现!<br></br>
        /// 是否启用多租户<br></br>
        /// 默认值: true<br></br>
        /// 否: 隐藏sass相关功能入口且所有功能以宿主身份运行
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
