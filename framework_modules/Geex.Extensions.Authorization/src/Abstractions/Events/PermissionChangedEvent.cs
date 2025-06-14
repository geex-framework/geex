
using MediatX;

namespace Geex.Extensions.Authorization.Events
{
    public record PermissionChangedEvent(string SubId, string[] Permissions) : IEvent
    {
        public string SubId { get; init; } = SubId;
        public string[] Permissions { get; init; } = Permissions;
    }
}
