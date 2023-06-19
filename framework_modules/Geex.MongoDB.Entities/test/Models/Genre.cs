using System;

namespace MongoDB.Entities.Tests
{
    public class Genre : EntityBase<Genre>
    {
        public string Name { get; set; }
        public Guid GuidId { get; set; }
        public int Position { get; set; }
        public double SortScore { get; set; }
        public Review Review { get; set; }

        [InverseSide]
        public Many<Book> Books { get; set; }

        public Genre() => this.InitManyToMany(x => Books, b => b.Genres);
    }
}
