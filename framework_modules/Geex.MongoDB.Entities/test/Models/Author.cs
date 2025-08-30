using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    [Name("Writer")]
    public class Author : EntityBase<Author>
    {
        public string Name { get; set; }
        public string Surname { get; set; }

        [Bson.Serialization.Attributes.BsonIgnoreIfNull]
        public string FullName { get; set; }

        public Date Birthday { get; set; }

        public int Age { get; set; }

        [Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        public int Age2 { get; set; }

        public IQueryable<Book> Books => LazyQuery(() => Books);

        public List<string> BookIds { get; set; } = new List<string>();

        public Author() => this.ConfigLazyQuery(x => Books, book => book.MainAuthorId == this.Id, authors => book => authors.SelectMany(x => x.BookIds).Distinct().Contains(book.Id));
    }
}
