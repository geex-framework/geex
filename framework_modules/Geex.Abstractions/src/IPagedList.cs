namespace Geex.Abstractions
{
    public interface IPagedList
    {
        int PageIndex { get; }
        int PageSize { get; }
        int TotalPage { get; }
        int TotalCount { get; }
    }
}
