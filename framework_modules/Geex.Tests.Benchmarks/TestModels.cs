using MongoDB.Bson.Serialization.Attributes;
using Geex;

namespace Geex.Tests.Benchmarks;

/// <summary>
/// 测试用的简单数据模型
/// </summary>
public class TestModel
{
    public string Name { get; set; } = "Test";
    public int Value { get; set; } = 42;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public string GetDisplayName() => $"{Name}_{Value}";
    public void UpdateValue(int newValue) => Value = newValue;
    public static string FormatValue(string input) => $"Formatted_{input}";
    public static T GenericMethod<T>(T input) => input;
}

/// <summary>
/// 测试用的BSON模型
/// </summary>
public class BsonTestModel
{
    [BsonElement("name")]
    public string Name { get; set; } = "BsonTest";
    
    [BsonElement("value")]
    public int Value { get; set; } = 100;
    
    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new() { "tag1", "tag2" };
}

/// <summary>
/// 测试用的枚举类
/// </summary>
public class TestEnumeration : Enumeration<TestEnumeration>
{
    public static readonly TestEnumeration Active = new("Active");
    public static readonly TestEnumeration Inactive = new("Inactive");
    public static readonly TestEnumeration Pending = new("Pending");

    public TestEnumeration(string value) : base(value) { }
    
    // 无参构造函数用于动态创建
    public TestEnumeration() { }
}

/// <summary>
/// 另一个测试枚举
/// </summary>
public class StatusEnumeration : Enumeration<StatusEnumeration>
{
    public static readonly StatusEnumeration New = new("New");
    public static readonly StatusEnumeration Processing = new("Processing");
    public static readonly StatusEnumeration Completed = new("Completed");

    public StatusEnumeration(string value) : base(value) { }
    public StatusEnumeration() { }
}
