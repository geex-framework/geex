using _org_._proj_._mod_.Api.Aggregates._aggregate_s;

using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;

namespace _org_._proj_._mod_.Core.Aggregates._aggregate_s
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public class _aggregate_ : Entity, I_aggregate_
    {
        public _aggregate_(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
