using System;
using MediatR;

namespace Geex.Bms.Demo.Core.GqlSchemas.Books.Inputs
{
    public class EditBookInput : IRequest<Unit>
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
