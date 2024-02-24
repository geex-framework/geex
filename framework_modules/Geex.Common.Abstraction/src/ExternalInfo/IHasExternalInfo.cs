using System.Text.Json.Nodes;

namespace Geex.Common.Abstraction.ExternalInfo
{
    /// <summary>
    /// 动态扩展的系统内对象无逻辑交互的信息, 通常是外部系统标记, 如"WorkflowId"等
    /// </summary>
    /// <typeparam name="TExternalInfo"></typeparam>
    public interface IHasExternalInfo<TExternalInfo>
    {
        /// <summary>
        /// 额外信息
        /// </summary>
        public TExternalInfo ExternalInfo { get; set; }
    }
    /// <summary>
    /// 动态扩展的系统内对象无逻辑交互的信息, 通常是外部系统标记, 如"WorkflowId"等
    /// </summary>
    public interface IHasExternalInfo
    {
        /// <summary>
        /// 额外信息
        /// </summary>
        public JsonNode? ExternalInfo { get; set; }
    }
}
