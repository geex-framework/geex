using Geex;
using Geex.Gql.AutoBatchLoad;

using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Configuration;

public static class TypeCompletionContextExtensions
{
    public static bool IsAutoBatchLoadEnabled(this ITypeCompletionContext completionContext) =>
        completionContext.Services.GetService(typeof(GeexCoreModuleOptions)) is GeexCoreModuleOptions options
            ? options.AutoBatchLoad
            : true;
}
