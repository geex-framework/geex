using MediatR;

namespace Geex.Common.Abstraction.Notifications;

/// <summary>
/// entity被删除的领域事件, 仅对geexEntity生效
/// </summary>
public record EntityDeletedNotification<T>(string EntityId) : INotification;