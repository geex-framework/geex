using System;

namespace Geex.Common.Abstraction.MultiTenant
{
    public interface ICurrentTenant
    {
        public string? Code { get; }
        public ITenant Detail { get; }
        public IDisposable Change(string? tenantCode);
    }
}
