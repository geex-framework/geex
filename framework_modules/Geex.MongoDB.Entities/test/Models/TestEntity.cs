using System;

namespace MongoDB.Entities.Tests.Models
{
    public class TestEntity : EntityBase<TestEntity>, ITestEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public TestEntityEnum Enum { get; set; }
        public int[] Data { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTime DateTime { get; set; }
    }

    public enum TestEntityEnum
    {
        Default = 0,
        Value1 = 1,
    }

    public record TestEntitySelectSubset
    {
        public TestEntitySelectSubset(string SelectName, int SelectValue, TestEntityEnum SelectEnum)
        {
            this.SelectName = SelectName;
            this.SelectValue = SelectValue;
            this.SelectEnum = SelectEnum;
        }

        public string SelectName { get; init; }
        public int SelectValue { get; init; }
        public TestEntityEnum SelectEnum { get; init; }

        public void Deconstruct(out string SelectName, out int SelectValue, out TestEntityEnum SelectEnum)
        {
            SelectName = this.SelectName;
            SelectValue = this.SelectValue;
            SelectEnum = this.SelectEnum;
        }
    }

    public interface ITestEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
