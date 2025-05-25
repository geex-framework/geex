
using MediatR;

namespace Geex.Extensions.Authorization.Events
{
    public record PermissionChangedEvent(string SubId, string[] Permissions) : INotification
    {
        public string SubId { get; init; } = SubId;
        public string[] Permissions { get; init; } = Permissions;
    }
}
