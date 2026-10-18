using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Entities;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Core.Entities;

internal static class ApprovalFlowConcurrency
{
    internal static async Task Guard<T>(IUnitOfWork uow, string id, string previous, string next,
        Expression<Func<T, string>> revision) where T : IEntityBase
    {
        if (previous == next) return;
        if (!uow.DbContext.SupportTransaction || !uow.DbContext.Session.IsInTransaction)
            throw new UserFriendlyException("审批流程保存需要数据库事务支持.");
        var filter = Builders<T>.Filter;
        var expected = filter.Eq(revision, previous);
        if (string.IsNullOrEmpty(previous)) expected |= filter.Exists(nameof(ApprovalFlow.Revision), false);
        // 在保存事务内取得条件写锁, 将办理前提与后续实体更新绑定到同一版本.
        var result = await uow.DbContext.Collection<T>().UpdateOneAsync(uow.DbContext.Session,
            filter.Eq(x => x.Id, id) & expected, Builders<T>.Update.Set(revision, next));
        if (result.MatchedCount != 1)
            throw new UserFriendlyException("流程已被其他操作更新, 请刷新后重试.");
    }
}
