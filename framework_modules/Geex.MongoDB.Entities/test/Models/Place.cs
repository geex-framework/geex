using System;

namespace MongoDB.Entities.Tests
{
    public class Place : EntityBase<Place>
    {
        public string Name { get; set; }
        public Coordinates2D Location { get; set; }
        public double DistanceKM { get; set; }
    }
}
