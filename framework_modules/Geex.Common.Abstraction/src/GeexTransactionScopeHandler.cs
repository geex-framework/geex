using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;

namespace Geex.Common
{
    public class GeexTransactionScopeHandler : ITransactionScopeHandler
    {
        public virtual ITransactionScope Create(IRequestContext context)
        {
            return new GeexTransactionScope(context, new TransactionScope(TransactionScopeOption.Required, new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
            }, TransactionScopeAsyncFlowOption.Enabled));
        }
    }

    public class GeexTransactionScope : ITransactionScope
    {
        public GeexTransactionScope(IRequestContext context, TransactionScope transaction)
        {
            this.Context = context;
            this.Transaction = transaction;
            this.UowServices = context.Services.GetServices<IUnitOfWork>();
        }

        public IEnumerable<IUnitOfWork> UowServices { get; set; }

        protected IRequestContext Context { get; }

        protected TransactionScope Transaction { get; }

        public void Complete()
        {
            try
            {
                bool flag;
                if (this.Context.Result is QueryResult result && result.Data != null)
                {
                    IReadOnlyList<IError> errors = result.Errors;
                    if (errors == null || errors.Count == 0)
                    {
                        flag = true;
                        goto label_4;
                    }
                }
                flag = false;
                label_4:
                if (!flag)
                    return;
                foreach (var unitOfWork in UowServices)
                {
                    unitOfWork.CommitAsync().Wait();
                }
                this.Transaction.Complete();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Dispose() => this.Transaction.Dispose();
    }

    public interface IUnitOfWork : IDisposable
    {

        Task CommitAsync(CancellationToken? cancellationToken = default);
        public event Func<Task>? OnCommitted;
    }
}