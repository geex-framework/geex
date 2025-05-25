using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Identity
{
    public record UserOrgChangedEvent(string UserId, List<string> Orgs) : INotification
    {
    }
}
