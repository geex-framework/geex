using Geex;

namespace Geex.Analyzer.Tests
{
    /// <summary>
    /// 测试枚举类，用于验证 EnumerationConstantsGenerator 的功能
    /// </summary>
    public class TestEnumeration : Enumeration<TestEnumeration>
    {
        public TestEnumeration(string directValue) : base(directValue)
        {

        }
        public const string _Value1 = "test_value1";
        public static TestEnumeration Value1 { get; } = FromNameAndValue(nameof(Value1), _Value1);

        public const string _Value2 = "test_value2";
        public static TestEnumeration Value2 { get; } = FromNameAndValue(nameof(Value2), _Value2);

        public const string _Value3 = "another_test";
        public static TestEnumeration Value3 { get; } = FromNameAndValue(nameof(Value3), _Value3);

        // 使用直接构造函数的情况
        public static TestEnumeration DirectValue { get; } = new("direct_value");
    }

    /// <summary>
    /// 另一个测试枚举类
    /// </summary>
    public class AnotherTestEnum : Enumeration<AnotherTestEnum>
    {
        public const string _Option1 = "option1";
        public static AnotherTestEnum Option1 { get; } = FromNameAndValue(nameof(Option1), _Option1);

        public const string _Option2 = "option2";
        public static AnotherTestEnum Option2 { get; } = FromNameAndValue(nameof(Option2), _Option2);
    }
}
