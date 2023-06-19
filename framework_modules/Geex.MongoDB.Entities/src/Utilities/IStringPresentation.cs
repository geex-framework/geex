using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Entities.Utilities
{
    public interface IStringPresentation
    {
        public string ToString()
        {
            return ((object)this).ToString();
        }
    }
}
