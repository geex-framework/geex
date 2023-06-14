using System;

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

        [Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        public One<Book> BestSeller { get; set; }

        public Many<Book> Books { get; set; }

        [ObjectId]
        public string BookIds { get; set; }

        public DateTimeOffset ModifiedOn { get; set; }

        public Author() => this.InitOneToMany(x => Books);
    }
}
