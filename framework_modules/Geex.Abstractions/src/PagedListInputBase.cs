namespace Geex.Abstractions
{
    public class PagedListQueryRequestBase : IPagedListQueryRequest
    {
        #region PageIndex
        private int _pageIndex = 1;
        protected virtual void SetPageIndex(int input)
        {
            if (input > 0)
            {
                _pageIndex = input;
            }
        }
        public int PageIndex
        {
            get
            {
                return _pageIndex;
            }
            set
            {
                SetPageIndex(value);
            }
        }

        #endregion

        #region PageSize
        private int _pageSize = 10;
        protected virtual void SetPageSize(int input)
        {
            if (input > 0)
            {
                _pageSize = input;
            }
        }
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                SetPageSize(value);
            }
        }
        #endregion
    }
}
