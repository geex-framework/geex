using MediatR;

namespace Geex.Common.Abstraction.Storage;

/// <summary>
/// entity被删除的领域事件, 仅对geexEntity生效
/// </summary>
/// <param name="Entity"></param>
public record EntityDeletedNotification<T>(T Entity) : INotification;