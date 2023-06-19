using Geex.Common.Abstraction.ExternalInfo;
using MongoDB.Entities;

namespace Geex.Common.Abstraction.MultiTenant
{
    public interface ITenant : IEntityBase, IHasExternalInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
    }
}
