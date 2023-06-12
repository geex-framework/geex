using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Geex.Common.Identity.Api.Aggregates.Orgs.Events
{
    public record UserOrgChangedEvent(string UserId, List<string> Orgs) : INotification
    {
    }
}
