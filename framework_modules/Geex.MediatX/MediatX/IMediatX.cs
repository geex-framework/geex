using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;

namespace MediatX
{
  public interface IMediatX
  {
    Task SendRemoteNotification<TRequest>(TRequest request) where TRequest : INotification;
  }
}
