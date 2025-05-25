using Geex.Common;
using Geex.Abstractions.Storage;

using MongoDB.Entities;

namespace Geex.Tests.TestEntities
{
    public class TestEntity : Entity<TestEntity>, ITestEntity
    {
        public TestEntity()
        {

        }
        public TestEntity(string name, int value, int[] data, DateTimeOffset dateTimeOffset, DateTime dateTime, IUnitOfWork? uow = default)
        {
            Name = name;
            Value = value;
            Data = data;
            DateTimeOffset = dateTimeOffset;
            DateTime = dateTime;
            uow?.Attach(this);
        }
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
