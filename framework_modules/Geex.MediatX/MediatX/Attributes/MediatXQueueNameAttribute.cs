using System;

namespace MediatX
{
  /// <summary>
  /// Represents an attribute used to specify the name of the MediatX queue for a class.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class MediatXQueueNameAttribute : System.Attribute
  {
    public string Name { get; set; }
  }
}
