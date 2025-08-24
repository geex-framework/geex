using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Bson;

namespace MongoDB.Entities.Tests
{
    [Name("Writer")]
    public class Author : EntityBase<Author>, IModifiedOn
    {
        public string Name { get; set; }
        public string Surname { get; set; }

        [Bson.Serialization.Attributes.BsonIgnoreIfNull]
        public string FullName { get; set; }

        [Preserve]
        public Date Birthday { get; set; }

        [Preserve]
        public int Age { get; set; }

        [Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        [Preserve]
        public int Age2 { get; set; }

        public IQueryable<Book> Books => LazyQuery(() => Books);

        public List<ObjectId> BookIds { get; set; } = new List<ObjectId>();

        public DateTimeOffset ModifiedOn { get; set; }

        public Author() => this.ConfigLazyQuery(x => Books, book => book.MainAuthorId == this.Id, authors => book => authors.SelectMany(x => x.BookIds).Distinct().Contains(book.Id));
    }
}
