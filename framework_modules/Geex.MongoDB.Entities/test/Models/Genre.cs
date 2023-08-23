using System;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    public class Genre : EntityBase<Genre>
    {
        public string Name { get; set; }
        public Guid GuidId { get; set; }
        public int Position { get; set; }
        public double SortScore { get; set; }
        public Review Review { get; set; }

        public IQueryable<Book> Books => LazyQuery(() => Books);

        public Genre() => this.ConfigLazyQuery(x => Books, book => book.GenreIds.Contains(this.Id), genres => book => genres.SelectMany(x => x.Books).SelectMany(x => x.GenreIds).Contains(Id));
    }
}
