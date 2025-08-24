using MediatX;

using MongoDB.Bson;

namespace Geex.Notifications;

/// <summary>
/// entity被删除的领域事件, 仅对geexEntity生效
/// </summary>
public record EntityDeletedEvent<T>(ObjectId EntityId) : IEvent;