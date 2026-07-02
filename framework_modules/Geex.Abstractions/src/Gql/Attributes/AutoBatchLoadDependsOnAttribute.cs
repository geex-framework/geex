using System;

namespace Geex.Gql.Attributes
{
    /// <summary>
    /// 声明计算属性在 getter 中间接依赖的 Lazy 导航属性名。
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>前端只查计算字段（如 <c>childCount</c>）、未直接查 Lazy 导航（如 <c>childNodes</c>）时，<c>AutoBatchLoad</c> 仍会为其预配 BatchLoad，避免 N+1。参见 <c>BatchLoadTestEntity</c>。</description></item>
    /// <item><description>补充 selection 驱动逻辑——后者仅处理 selection 中直接出现的 Lazy 导航。</description></item>
    /// <item><description>手动 <c>.BatchLoad()</c> 不受本特性影响；<c>AutoBatchLoad</c> 关闭时无运行时效果。</description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AutoBatchLoadDependsOnAttribute : Attribute
    {
        public AutoBatchLoadDependsOnAttribute(string navigationPropertyName)
        {
            NavigationPropertyName = navigationPropertyName;
        }

        public string NavigationPropertyName { get; }
    }
}
