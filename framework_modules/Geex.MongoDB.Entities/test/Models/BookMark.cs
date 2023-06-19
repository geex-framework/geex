namespace MongoDB.Entities.Tests.Models
{
    public class BookMark : EntityBase<BookMark>
    {
        public One<BookCover> BookCover { get; set; }
        public string BookName { get; set; }
    }
}
