namespace Geex.Extensions.MultiTenant.Api
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
