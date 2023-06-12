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
            string v1 = parms.Object1.ToString();
            if (parms.Object1 is IEnumeration e1)
            {
                v1 = e1.Value;
            }
            string v2 = parms.Object2.ToString();
            if (parms.Object2 is IEnumeration e2)
            {
                v2 = e2.Value;
            }

            if (v1 == v2)
            {
                return;
            }

            AddDifference(parms);
        }

        public override bool IsTypeMatch(Type type1, Type type2)
        {
            return type1?.IsAssignableTo<IEnumeration>() == true || type2?.IsAssignableTo<IEnumeration>() == true;
        }
    }
}