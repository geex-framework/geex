using System;

namespace Geex.MultiTenant
{
    public interface ICurrentTenant
    {
        public string? Code { get; }
        public ITenant Detail { get; }
        public IDisposable Change(string? tenantCode);
    }
}
