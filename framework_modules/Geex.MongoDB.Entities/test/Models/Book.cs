using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities.Tests
{
    public class Book : EntityBase<Book>
    {
        public Date PublishedOn { get; set; }

        [DontPreserve] public string Title { get; set; }
        [DontPreserve] public decimal Price { get; set; }

        public int PriceInt { get; set; }
        public long PriceLong { get; set; }
        public double PriceDbl { get; set; }
        public float PriceFloat { get; set; }
        public Author RelatedAuthor { get; set; }
        public Author[] OtherAuthors { get; set; }
        public Review Review { get; set; }
        public Review[] ReviewArray { get; set; }
        public string[] Tags { get; set; }
        public IList<Review> ReviewList { get; set; }
        public Lazy<Author> MainAuthor => LazyQuery(() => MainAuthor);

        public IQueryable<Author> GoodAuthors => LazyQuery(() => GoodAuthors);
        public IQueryable<Author> BadAuthors => LazyQuery(() => BadAuthors);

        public IQueryable<Genre> Genres => LazyQuery(() => Genres);

        [Ignore]
        public int DontSaveThis { get; set; }

        public List<string> GoodAuthorIds { get; set; } = new List<string>();

        public Book()
        {
            this.ConfigLazyQuery(x => x.GoodAuthors, author => this.GoodAuthorIds.Contains(author.Id), books => author => books.SelectMany(x => x.GoodAuthorIds).Contains(author.Id));
            this.ConfigLazyQuery(x => x.BadAuthors, author => this.BadAuthorIds.Contains(author.Id), books => author => books.SelectMany(x => x.BadAuthorIds).Contains(author.Id));
            this.ConfigLazyQuery(x => x.Genres, author => this.GenreIds.Contains(author.Id), books => author => books.SelectMany(x => x.GenreIds).Contains(author.Id));
            this.ConfigLazyQuery(x => x.MainAuthor, author => this.MainAuthorId == (author.Id), books => author => books.SelectList(x => x.MainAuthorId).Contains(author.Id));
        }

        public List<string> BadAuthorIds { get; set; } = new List<string>();
        public List<string> GenreIds { get; set; } = new List<string>();
        public string MainAuthorId { get; set; }
    }
}
