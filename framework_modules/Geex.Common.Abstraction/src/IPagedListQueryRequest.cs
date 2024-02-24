namespace Geex.Common.Abstraction
{
    public interface IPagedListQueryRequest
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
    }
}
