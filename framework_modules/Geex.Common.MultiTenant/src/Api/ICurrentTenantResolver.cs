using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geex.Common.MultiTenant.Api
{
    /// <summary>
    /// 租户解析器
    /// </summary>
    public interface ICurrentTenantResolver
    {
        /// <summary>
        /// 租户解析优先级 query>header>cookie
        /// </summary>
        /// <returns></returns>
        string? Resolve();
    }
}
