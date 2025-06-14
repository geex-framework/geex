using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Identity
{
    public record UserOrgChangedEvent(string UserId, List<string> Orgs) : IEvent
    {
    }
}
