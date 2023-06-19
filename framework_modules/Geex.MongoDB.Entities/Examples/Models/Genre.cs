using MongoDB.Entities;

namespace Examples.Models
{
    public class Genre : EntityBase<Genre>
    {
        public string Name { get; set; }

        [InverseSide]
        public Many<Book> AllBooks { get; set; }

        public Genre() => this.InitManyToMany(x => AllBooks, book => book.AllGenres);
    }
}
