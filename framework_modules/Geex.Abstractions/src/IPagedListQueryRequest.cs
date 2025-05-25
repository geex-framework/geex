namespace Geex.Abstractions
{
    public interface IPagedListQueryRequest
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
    }
}
