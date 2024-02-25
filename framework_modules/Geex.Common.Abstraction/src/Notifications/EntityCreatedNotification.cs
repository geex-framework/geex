using MediatR;

namespace Geex.Common.Notifications;

/// <summary>
/// entity创建的领域事件, 仅对geexEntity生效
/// </summary>
/// <param name="Entity"></param>
public record EntityCreatedNotification<T>(T Entity) : INotification;