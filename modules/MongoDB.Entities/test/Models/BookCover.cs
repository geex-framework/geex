using MongoDB.Bson;

namespace MongoDB.Entities.Tests.Models
{
    public class BookCover : EntityBase<BookCover>
    {
        public string BookName { get; set; }
        public ObjectId BookId { get; set; }
        public Many<BookMark> BookMarks { get; set; }

        public BookCover()
        {
            this.InitOneToMany(x => BookMarks);
        }
    }
}
