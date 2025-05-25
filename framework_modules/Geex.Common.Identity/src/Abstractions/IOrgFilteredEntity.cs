using MongoDB.Entities;

namespace Geex.Common.Identity;

/// <summary>
/// 授权过滤接口
/// </summary>
public interface IOrgFilteredEntity : IEntityBase
{
    public string OrgCode { get; }
}