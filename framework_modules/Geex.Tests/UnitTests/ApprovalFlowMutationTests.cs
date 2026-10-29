using System.Reflection;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Gql;
using Volo.Abp;
using Xunit;

namespace Geex.Tests.UnitTests;

public sealed class ApprovalFlowMutationTests
{
    [Fact]
    public async Task Close_missing_node_returns_a_business_error_before_any_write()
    {
        var work = DispatchProxy.Create<IUnitOfWork, EmptyNodeQuery>();
        var mutation = new ApprovalFlowMutation(work);

        var error = await Assert.ThrowsAsync<UserFriendlyException>(
            () => mutation.CloseApprovalFlow("000000000000000000000099"));

        Assert.Equal("审批节点不存在或无权访问.", error.Message);
        Assert.Equal(1, ((EmptyNodeQuery)work).QueryCount);
    }

    public class EmptyNodeQuery : DispatchProxy
    {
        public int QueryCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IRepository.Query)
                && targetMethod.GetGenericArguments().SequenceEqual([typeof(ApprovalFlowNode)]))
            {
                QueryCount++;
                return Array.Empty<ApprovalFlowNode>().AsQueryable();
            }
            throw new NotSupportedException($"Unexpected unit of work operation: {targetMethod?.Name}");
        }
    }
}
