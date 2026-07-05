using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;
using Geex.Analyzer.CodeFixProviders;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using MongoDB.Entities;

using Xunit;

namespace Geex.Analyzer.Tests
{
    public class AutoBatchLoadDependsOnCodeFixProviderTests
    {
        [Fact]
        public async Task MissingDependsOn_ShouldAddAutoBatchLoadDependsOnAttribute()
        {
            var source = await ProjectBasedAnalyzerVerifier<AutoBatchLoadDependsOnAnalyzer>
                .GetSourceCodeForTestAsync("AutoBatchLoadTests/AutoBatchLoadDependsOnTests/MissingDependsOn.cs");

            const string fixedCode = """
                using System.Linq;

                using Geex.Storage;

                using MongoDB.Entities;

                namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
                {
                    public class AutoBatchLoadDependsOnTestLineEntity : Entity<AutoBatchLoadDependsOnTestLineEntity>
                    {
                        public decimal Amount { get; set; }
                    }

                    public class AutoBatchLoadDependsOnMissingTestEntity : Entity<AutoBatchLoadDependsOnMissingTestEntity>
                    {
                        public AutoBatchLoadDependsOnMissingTestEntity()
                        {
                            ConfigLazyQuery(
                                x => x.Lines,
                                _ => true,
                                _ => _ => true);
                        }

                        [Geex.Gql.Attributes.AutoBatchLoadDependsOn(nameof(Lines))]
                        public decimal TotalAmount => Lines.Sum(x => x.Amount);

                        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
                    }
                }

                """;

            var test = new AutoBatchLoadDependsOnCodeFixTest
            {
                TestCode = source,
                FixedCode = fixedCode,
            };

            test.ExpectedDiagnostics.Add(
                DiagnosticResultBuilder
                    .Create(AutoBatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("TotalAmount", "Lines"));

            await test.RunAsync();
        }

        private sealed class AutoBatchLoadDependsOnCodeFixTest
            : CSharpCodeFixTest<AutoBatchLoadDependsOnAnalyzer, AutoBatchLoadDependsOnCodeFixProvider, GeexVerifier>
        {
            public AutoBatchLoadDependsOnCodeFixTest()
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net100;
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(EntityBase<>).Assembly.Location));
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(Geex.Gql.Attributes.AutoBatchLoadDependsOnAttribute).Assembly.Location));
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(Geex.Storage.Entity<>).Assembly.Location));
            }
        }
    }
}
