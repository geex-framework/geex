using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;
using Geex.Analyzer.CodeFixProviders;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using MongoDB.Entities;

using Xunit;

namespace Geex.Analyzer.Tests
{
    public class BatchLoadDependsOnCodeFixProviderTests
    {
        [Fact]
        public async Task MissingDependsOn_ShouldAddBatchLoadDependsOnAttribute()
        {
            var source = await ProjectBasedAnalyzerVerifier<BatchLoadDependsOnAnalyzer>
                .GetSourceCodeForTestAsync("AutoBatchLoadTests/BatchLoadDependsOnTests/MissingDependsOn.cs");

            const string fixedCode = """
                using System.Linq;

                using Geex.Storage;

                using MongoDB.Entities;

                namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
                {
                    public class BatchLoadDependsOnTestLineEntity : Entity<BatchLoadDependsOnTestLineEntity>
                    {
                        public decimal Amount { get; set; }
                    }

                    public class BatchLoadDependsOnMissingTestEntity : Entity<BatchLoadDependsOnMissingTestEntity>
                    {
                        public BatchLoadDependsOnMissingTestEntity()
                        {
                            ConfigLazyQuery(
                                x => x.Lines,
                                _ => true,
                                _ => _ => true);
                        }

                        [Geex.Gql.Attributes.BatchLoadDependsOn(nameof(Lines))]
                        public decimal TotalAmount => Lines.Sum(x => x.Amount);

                        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
                    }
                }

                """;

            var test = new BatchLoadDependsOnCodeFixTest
            {
                TestCode = source,
                FixedCode = fixedCode,
            };

            test.ExpectedDiagnostics.Add(
                DiagnosticResultBuilder
                    .Create(BatchLoadDependsOnAnalyzer.DiagnosticId)
                    .WithArguments("TotalAmount", "Lines"));

            await test.RunAsync();
        }

        private sealed class BatchLoadDependsOnCodeFixTest
            : CSharpCodeFixTest<BatchLoadDependsOnAnalyzer, BatchLoadDependsOnCodeFixProvider, GeexVerifier>
        {
            public BatchLoadDependsOnCodeFixTest()
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(EntityBase<>).Assembly.Location));
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(Geex.Gql.Attributes.BatchLoadDependsOnAttribute).Assembly.Location));
                TestState.AdditionalReferences.Add(
                    MetadataReference.CreateFromFile(typeof(Geex.Storage.Entity<>).Assembly.Location));
            }
        }
    }
}
