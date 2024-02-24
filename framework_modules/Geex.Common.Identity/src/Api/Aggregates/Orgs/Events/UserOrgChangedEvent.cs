using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Identity.Api.Aggregates.Orgs.Events
{
    public record UserOrgChangedEvent(string UserId, List<string> Orgs) : INotification
    {
    }
}
