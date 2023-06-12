using System;

namespace MongoDB.Entities.Tests
{
    public class Place : EntityBase<Place>, IModifiedOn
    {
        public string Name { get; set; }
        public Coordinates2D Location { get; set; }
        public double DistanceKM { get; set; }
        public DateTimeOffset ModifiedOn { get; set; }
    }
}
