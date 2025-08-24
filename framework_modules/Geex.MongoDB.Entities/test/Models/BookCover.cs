using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Entities.Tests.Models
{
    public class BookCover : EntityBase<BookCover>
    {
        public string BookName { get; set; }
        public ObjectId BookId { get; set; }
        public IQueryable<BookMark> BookMarks { get; set; }

        public BookCover()
        {
            this.ConfigLazyQuery(x => x.BookMarks, author => this.BookMarkIds.Contains(author.Id), books => author => books.SelectMany(x => x.BookMarkIds).Contains(author.Id));
        }

        public List<ObjectId> BookMarkIds { get; set; }
    }
}
