using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using MediatX;

// ReSharper disable once CheckNamespace
namespace Geex.Common
{
  public interface IRemoteNotificationHandler
  {

  }
  public interface IRemoteNotificationHandler<TNotification> : IRemoteNotificationHandler, INotificationHandler<TNotification> where TNotification : IRemoteNotification
  {

  }
}
