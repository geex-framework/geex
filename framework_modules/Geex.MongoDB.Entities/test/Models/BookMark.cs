using System;
using System.Linq;

namespace MongoDB.Entities.Tests.Models
{
    public class BookMark : EntityBase<BookMark>
    {
        public Lazy<BookCover> BookCover => LazyQuery(() => BookCover);
        public string BookName { get; set; }

        public BookMark()
        {
            this.ConfigLazyQuery(x => x.BookCover, bookCover => this.BookCoverId == bookCover.Id, bookMarks => bookCover => bookMarks.SelectList(x=>x.BookCoverId).Contains(bookCover.Id)).ConfigCascadeDelete();
        }

        public string BookCoverId { get; set; }
    }
}
