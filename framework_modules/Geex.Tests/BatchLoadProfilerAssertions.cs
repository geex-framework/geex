using System.Linq;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Tests;

internal static class BatchLoadProfilerAssertions
{
    public const string AutoBatchLoadNamespace = "AutoBatchLoad";
    public const string BatchLoadTestNamespace = "BatchLoadTest";

    public static int CountLogs(string namespaceFragment) =>
        DB.GetProfilerLogs().AsQueryable()
            .Count(x => x.ns != null && x.ns.Contains(namespaceFragment));
}
