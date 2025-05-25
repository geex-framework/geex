using System;

namespace Geex.Abstractions.MultiTenant
{
    public interface ICurrentTenant
    {
        public string? Code { get; }
        public ITenant Detail { get; }
        public IDisposable Change(string? tenantCode);
    }
}
