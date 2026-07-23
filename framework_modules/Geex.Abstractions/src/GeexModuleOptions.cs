using System;

using Microsoft.Extensions.Configuration;

namespace Geex
{
  [Obsolete("please use GeexModuleOptions without generic param instead.")]
  public abstract class GeexModuleOptions<T> : GeexModuleOptions where T : GeexModule
  {
  }

  public class GeexModuleOptions
  {
    /// <summary>
    /// Options读取的appsettings的配置节点, 默认<see cref="GetType().Name"/>
    /// </summary>
    public virtual string BindSection => this.GetType().Name;

    /// <summary>
    /// When false, the module skips MediatR / GraphQL assembly registration in <see cref="GeexModule{TModule, TModuleOptions}.ConfigureServices"/>.
    /// Default true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public IConfiguration ConfigurationSection { get; internal set; }
  }
}
