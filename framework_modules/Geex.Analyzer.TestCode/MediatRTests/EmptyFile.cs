namespace Geex.Analyzer.TestCode.MediatRTests
{
    // 空文件或不包含 MediatR 相关代码的文件 - 不应该报告任何诊断
    public class EmptyFileTest
    {
        public void SomeMethod()
        {
            // 普通的方法，没有使用 MediatR
            var value = "Hello World";
            System.Console.WriteLine(value);
        }
    }
}
