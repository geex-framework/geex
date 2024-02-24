using System.Reflection;

namespace System.Linq
{
    public interface IBatchLoadQueryable : IQueryable
    {
        PropertyInfo ParentProp { get; set; }
        public IQueryable ParentQuery { get; set; }
    }
    public interface IBatchLoadQueryable<out TSource, TRelated> : IBatchLoadQueryable, IQueryable<TSource>
    {
    }
}