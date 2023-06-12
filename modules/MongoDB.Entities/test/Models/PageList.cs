using System.Collections.Generic;
using System.Linq;

public class PagedList<T>
    {
        public PagedList(IQueryable<T> dataSource, int pageIndex, int pageSize)
        {
            this.DataSource = dataSource;
            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
        }

        #region PageIndex
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

        #endregion

        #region PageSize
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
        #endregion

        #region PageCount
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
        #endregion

        #region TotalItemCount

        public int TotalCount => _totalCount ??= DataSource.Count();

        #endregion
    }