using System.Collections.Generic;
using MediatR;

namespace Geex.Extensions.Identity
{
    public record UserOrgChangedEvent(string UserId, List<string> Orgs) : INotification
    {
    }
}
