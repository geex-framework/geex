using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Storage;

namespace Geex.Bms.Demo.Core.Aggregates.books
{
    public class BookCategory: Entity<BookCategory>
    {

        #region Methods 方法
        public BookCategory(string name)
        {
            Name = name;
        }
        #endregion


        #region Properties 属性
        public string Name { get; set; }
        public string? Describe { get; set; }
        #endregion


    }
}
