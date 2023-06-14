using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.InnerQuery
{
    class GroupingDocument<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public GroupingDocument()
        {
            Values = new List<TElement>();
        }

        public List<TElement> Values { get; set; }
        public TKey Key { get; set; }

        public IEnumerator<TElement> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
