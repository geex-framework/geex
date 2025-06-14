using MediatX;

namespace Geex.Notifications;

/// <summary>
/// entity被删除的领域事件, 仅对geexEntity生效
/// </summary>
public record EntityDeletedEvent<T>(string EntityId) : IEvent;