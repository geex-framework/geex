using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MongoDB.Entities;

public partial class DbContext
{
    public event Func<CancellationToken, Task>? PersistingTransaction;
    public event Action? TransactionAborted;
    private readonly Dictionary<object, Func<Task>> _afterCommit = new();
    private bool _transactionAborted;

    protected void ThrowIfTransactionAborted()
    {
        if (_transactionAborted) throw new InvalidOperationException("This unit of work was rolled back. Retry in a new unit of work.");
    }

    public void AfterCommit(object key, Func<Task> action) => _afterCommit[key] = action;

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfTransactionAborted();
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await Session.CommitTransactionAsync(cancellationToken);
                break;
            }
            catch (MongoException error) when (attempt < 2 && error.HasErrorLabel("UnknownTransactionCommitResult")) { }
        }
        IsInExplicitTransaction = false;
        var callbacks = _afterCommit.Values.ToArray();
        _afterCommit.Clear();
        foreach (var action in callbacks)
        {
            try { await action(); }
            catch (Exception error) { Logger.LogError(error, "A post-commit cache update failed."); }
        }
    }

    public async Task AbortTransactionAsync(CancellationToken cancellationToken = default)
    {
        _afterCommit.Clear();
        _transactionAborted = true;
        IsInExplicitTransaction = false;
        if (Session.IsInTransaction) await Session.AbortTransactionAsync(cancellationToken);
        TransactionAborted?.Invoke();
    }
}
