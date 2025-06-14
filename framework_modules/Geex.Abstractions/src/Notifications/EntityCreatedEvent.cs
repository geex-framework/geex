using MediatX;

namespace Geex.Notifications;

/// <summary>
/// entity创建的领域事件, 仅对geexEntity生效
/// </summary>
/// <param name="entity"></param>
public record EntityCreatedEvent<T>(T entity) : IEvent;