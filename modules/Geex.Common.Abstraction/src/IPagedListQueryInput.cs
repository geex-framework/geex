using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geex.Common.Abstraction
{
    public interface IPagedListQueryInput
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
    }
}
