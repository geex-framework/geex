namespace MongoDB.Entities.Tests.Models
{
    public class Flower : EntityBase<Flower>
    {
        public string Name { get; set; }
        public string Color { get; set; }

        public Flower()
        {
        }
    }
}
