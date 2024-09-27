using Geex.Common.Abstraction.Storage;
using MongoDB.Entities;

namespace Geex.Tests.TestEntities
{
    public class TestEntity : Entity<TestEntity>, ITestEntity
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
