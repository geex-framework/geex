using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

// ReSharper disable once CheckNamespace
namespace MediatR
{
  public interface IRemoteNotificationHandler
  {

  }
  public interface IRemoteNotificationHandler<TNotification> : IRemoteNotificationHandler, INotificationHandler<TNotification> where TNotification : INotification
  {

  }
}
