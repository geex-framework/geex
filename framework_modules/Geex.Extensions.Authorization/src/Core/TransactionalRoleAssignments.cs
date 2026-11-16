using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authorization.Core.Casbin;
using MongoDB.Driver;

namespace Geex.Extensions.Authorization.Core;

public sealed class TransactionalRoleAssignments
{
    private readonly IUnitOfWork _uow;
    private readonly IRbacEnforcer _enforcer;
    private readonly Dictionary<string, List<string>> _pending = new();

    public TransactionalRoleAssignments(IUnitOfWork uow, IRbacEnforcer enforcer)
    {
        _uow = uow;
        _enforcer = enforcer;
        uow.DbContext.PersistingTransaction += Persist;
        uow.DbContext.TransactionAborted += () => _pending.Clear();
    }

    public void Set(string userId, IEnumerable<string> roles) => _pending[userId] = roles.Distinct().ToList();

    private async Task Persist(CancellationToken cancellationToken)
    {
        if (_pending.Count == 0) return;
        if (!_uow.DbContext.Session.IsInTransaction)
            throw new InvalidOperationException("User role assignments require a database transaction.");
        var snapshot = _pending.ToDictionary(x => x.Key, x => x.Value.ToList());
        var collection = _uow.DbContext.Collection<CasbinRule>();
        foreach (var (userId, roles) in snapshot)
        {
            await collection.DeleteManyAsync(_uow.DbContext.Session,
                x => x.PType == "g" && x.V0 == userId, cancellationToken: cancellationToken);
            if (roles.Count > 0)
                await collection.InsertManyAsync(_uow.DbContext.Session,
                    roles.Select(role => new CasbinRule { PType = "g", V0 = userId, V1 = role,
                        CreatedOn = DateTimeOffset.UtcNow }), cancellationToken: cancellationToken);
        }
        _uow.DbContext.AfterCommit(this, async () =>
        {
            foreach (var userId in snapshot.Keys) await _enforcer.ReloadUserRoles(userId);
            _pending.Clear();
        });
    }
}
