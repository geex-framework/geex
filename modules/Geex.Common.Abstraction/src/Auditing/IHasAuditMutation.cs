using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using HotChocolate;

using MediatR;

namespace Geex.Common.Abstraction.Auditing
{
    public interface IHasAuditMutation
    {
    }
    public interface IHasAuditMutation<T> : IHasAuditMutation where T : IAuditEntity
    {
        async Task<bool> Submit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new SubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> Audit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new AuditRequest<T>(remark, ids));
            return true;
        }
        async Task<bool> UnSubmit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new UnsubmitRequest<T>(remark, ids));
            return true;
        }

        async Task<bool> UnAudit([Service] IMediator mediator, string[] ids, string? remark)
        {
            await mediator.Send(new UnauditRequest<T>(remark, ids));
            return true;
        }
    }
}
