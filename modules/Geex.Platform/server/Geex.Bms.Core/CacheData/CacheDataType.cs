using Geex.Common.Abstractions;

namespace Geex.Bms.Core.CacheData;
/// <summary>
/// 缓存数据变更类型
/// </summary>
public class CacheDataType : Enumeration<CacheDataType>
{
    public CacheDataType(string name, string value) : base(name, value)
    {

    }
    public static CacheDataType Org { get; } = new(nameof(Org), nameof(Org));

}
