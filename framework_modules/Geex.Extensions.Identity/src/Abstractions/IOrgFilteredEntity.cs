using MongoDB.Entities;

namespace Geex.Extensions.Identity;

/// <summary>
/// 授权过滤接口
/// </summary>
public interface IOrgFilteredEntity : IEntityBase
{
    public string OrgCode { get; }
}
