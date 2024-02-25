using MediatR;

namespace Geex.Common.Notifications;

/// <summary>
/// entity被删除的领域事件, 仅对geexEntity生效
/// </summary>
public record EntityDeletedNotification<T>(string EntityId) : INotification;