﻿using System;
using Geex.Storage;

namespace MongoDB.Entities.Tests.Models
{
    public class TestEntity : Entity<TestEntity>, ITestEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public TestEntityEnum Enum { get; set; }
        public int ValuePlus1 => Value + 1;
        public int[] Data { get; set; }
        public DateTimeOffset? DateTimeOffset { get; set; }
        public DateTime DateTime { get; set; }
    }

    public enum TestEntityEnum
    {
        Default = 0,
        Value1 = 1,
    }

    public record TestEntitySelectSubset
    {
        public TestEntitySelectSubset()
        {

        }
        public TestEntitySelectSubset(string SelectId,string SelectName, int SelectValue, TestEntityEnum SelectEnum)
        {
            this.SelectId = SelectId;
            this.SelectName = SelectName;
            this.SelectValue = SelectValue;
            this.SelectEnum = SelectEnum;
        }

        public string SelectId { get; init;}
        public string SelectName { get; init; }
        public int SelectValue { get; init; }
        public TestEntityEnum SelectEnum { get; init; }
        public DateTimeOffset? SelectDateTimeOffset { get; init; }

        public void Deconstruct(out string SelectId,out string SelectName, out int SelectValue, out TestEntityEnum SelectEnum)
        {
            SelectId = this.SelectId;
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
