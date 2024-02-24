using System;

namespace MongoDB.Entities.Tests.Models
{
    public class TestEntity : EntityBase<TestEntity>, ITestEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public int[] Data { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTime DateTime { get; set; }
    }

    public interface ITestEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
