using System;

using Microsoft.Extensions.Configuration;

namespace Geex.Common.Abstractions
{
    public abstract class GeexModuleOption<T> : GeexModuleOption where T : GeexModule
    {

    }

    public class GeexModuleOption
    {
        public IConfiguration ConfigurationSection { get; internal set; }
    }
}