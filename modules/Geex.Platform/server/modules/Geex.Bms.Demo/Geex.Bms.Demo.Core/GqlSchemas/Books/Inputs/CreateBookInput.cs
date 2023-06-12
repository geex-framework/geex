using System;
using MediatR;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs
{
    public class CreateBookInput : IRequest<Book>
    {
        public string Name { get; set; }
        public string Cover { get; set; }
        public string Author { get; set; }
        public string Press { get; set; }
        public DateTimeOffset PublicationDate { get; set; }
        public string ISBN { get; set; }
        public string BookCategoryId { get; set; }
    }
}
