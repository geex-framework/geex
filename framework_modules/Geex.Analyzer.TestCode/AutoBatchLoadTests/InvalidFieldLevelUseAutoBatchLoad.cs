using HotChocolate.Types;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public static class InvalidFieldLevelExtensions
    {
        public static IObjectFieldDescriptor UseAutoBatchLoad(this IObjectFieldDescriptor descriptor, bool enabled) =>
            descriptor;
    }

    public class InvalidFieldLevelUseAutoBatchLoadSample
    {
        public void Configure(IObjectFieldDescriptor field)
        {
            field.UseAutoBatchLoad(false);
        }
    }
}
