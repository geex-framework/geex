using System;
using System.Collections.Generic;
using System.Linq;
using Geex.Storage;

namespace Geex.Analyzer.TestCode.QueryTests
{
    // 测试实体
    public class TestEntity : Entity<TestEntity>
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public TimeSpan Duration { get; set; }
        public string Category { get; set; }
        public string SearchTerm { get; set; }
    }

    public class UserEntity : Entity<UserEntity>
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
    }

    // 非实体类 - 用于验证分析器不会对非实体类报告诊断
    public class RegularClass
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }
}
