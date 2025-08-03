using MediatX;

// ReSharper disable once CheckNamespace
namespace Geex.Common
{
  public interface IDistributedEventHandler
  {

  }
  public interface IDistributedEventHandler<in TEvent> : IDistributedEventHandler, IEventHandler<TEvent> where TEvent : IDistributedEvent
  {

  }
}
