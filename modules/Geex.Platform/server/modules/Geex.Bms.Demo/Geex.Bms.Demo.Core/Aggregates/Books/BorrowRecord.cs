using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Storage;

namespace Geex.Bms.Demo.Core.Aggregates.books
{
    public class BorrowRecord: Entity<BorrowRecord>
    {

        #region Methods 方法
        public BorrowRecord(Reader readers, Book book):this()
        {
            ReaderId = readers.Id;

            BookId = book.Id;

            book.Lending();

            ReadersDate = DateTime.Now;
        }

        private BorrowRecord()
        {
         
            Reader = new ResettableLazy<Reader?>(() => DbContext.Queryable<Reader>().GetById(this.ReaderId));
            Book = new ResettableLazy<Book?>(() => DbContext.Queryable<Book>().GetById(this.BookId));
        }


        public void BookReturn()
        {
            ReturnDate = DateTime.Now;
            this.Book.Value?.Return();
        }

        #endregion


        #region Properties 属性

        public virtual string ReaderId { get; set; }
        public virtual string BookId { get; set; }
        public DateTime ReadersDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public virtual ResettableLazy<Reader?> Reader  { get; }
        public virtual ResettableLazy<Book?> Book  { get; }

        #endregion


    }
}
