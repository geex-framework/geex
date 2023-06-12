using MongoDB.Entities;

namespace Examples.Models
{
    public class Author : EntityBase<Author>
    {
        public string Name { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Book> Books { get; set; }

        public Author() => this.InitOneToMany(x => Books);
    }
}
