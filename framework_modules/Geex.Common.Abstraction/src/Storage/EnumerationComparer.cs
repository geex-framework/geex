using System;

using Geex.Common.Abstractions;

using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

namespace Geex.Common.Abstraction.Storage
{
    internal class EnumerationComparer : BaseTypeComparer
    {
        public EnumerationComparer(RootComparer rootComparer) : base(rootComparer)
        {
        }

        public override void CompareType(CompareParms parms)
        {
            var v1 = GetValue(parms.Object1);
            var v2 = GetValue(parms.Object2);

            if (v1 == v2)
            {
                return;
            }

            AddDifference(parms);
        }

        private string? GetValue(object? obj)
        {
            if (obj is IEnumeration e)
            {
                return e.Value;
            }

            return obj?.ToString();
        }

        public override bool IsTypeMatch(Type type1, Type type2)
        {
            return type1?.IsAssignableTo<IEnumeration>() == true || type2?.IsAssignableTo<IEnumeration>() == true;
        }
    }
}