using System.Collections.Generic;
using System.Linq;

namespace Geex.Common.Abstraction
{
    public class PagedList<T> : IPagedList
    {
        public PagedList(IQueryable<T> dataSource, IPagedListQueryRequest request)
        {
            this.DataSource = dataSource;
            this.PageIndex = request.PageIndex;
            this.PageSize = request.PageSize;
        }

        private int _pageIndex = 1;
        protected virtual void SetPageIndex(int input)
        {
            if (input > 0)
            {
                _pageIndex = input;
            }
        }

        private IQueryable<T> DataSource { get; }

        public int PageIndex
        {
            get => _pageIndex;
            private init => SetPageIndex(value);
        }


        private int _pageSize = 10;
        private int? _totalCount;
        private List<T>? _items;

        protected virtual void SetPageSize(int input)
        {
            if (input > 0)
            {
                _pageSize = input;
            }
        }
        public int PageSize
        {
            get => _pageSize;
            private init => SetPageSize(value);
        }

        public List<T> Items => _items ??= DataSource.Skip((PageIndex - 1) * PageSize).Take(PageSize).ToList();

        public int TotalPage
        {
            get
            {
                var totalCount = TotalCount;
                if (totalCount < 1)
                {
                    return 0;
                }
            if (PageSize <= 1)
                {
                    return totalCount;
                }
                var pageCount = totalCount / PageSize;
                if (totalCount % PageSize > 0)
                {
                    pageCount++;
                }
                return pageCount;
            }
        }

        public int TotalCount => _totalCount ??= DataSource.Count();
    }
}
