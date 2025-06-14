using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
