using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using KellermanSoftware.CompareNetObjects.TypeComparers;

using KellermanSoftware.CompareNetObjects;

namespace Geex.MongoDB.Entities.Utilities
{
    public class GeexByteArrayComparer : BaseTypeComparer
    {
        /// <summary>
        /// Protected constructor that references the root comparer
        /// </summary>
        /// <param name="rootComparer">The root comparer.</param>
        public GeexByteArrayComparer(RootComparer rootComparer)
          : base(rootComparer)
        {
        }

        /// <summary>
        /// If true the type comparer will handle the comparison for the type
        /// </summary>
        /// <param name="type1">The type of the first object</param>
        /// <param name="type2">The type of the second object</param>
        /// <returns><c>true</c> if it is a byte array; otherwise, <c>false</c>.</returns>
        public override bool IsTypeMatch(Type type1, Type type2) => TypeHelper.IsByteArray(type1) && TypeHelper.IsByteArray(type2);

        /// <summary>Compare two byte array objects</summary>
        public override void CompareType(CompareParms parms)
        {
            if (parms == null || parms.Object1 == null || parms.Object2 == null || this.ListsDifferent(parms))
                return;
        }

        private bool ListsDifferent(CompareParms parms)
        {
            IList object1 = parms.Object1 as IList;
            IList object2 = parms.Object2 as IList;
            if (object1 == null)
                throw new ArgumentException("parms.Object1");
            if (object2 == null)
                throw new ArgumentException("parms.Object2");
            if (object1.Count != object2.Count)
            {
                this.AddDifference(parms.Result, new Difference());

                Difference difference1 = new Difference();
                difference1.ParentObject1 = parms.ParentObject1;
                difference1.ParentObject2 = parms.ParentObject2;
                difference1.PropertyName = parms.BreadCrumb;
                difference1.ChildPropertyName = "Count";
                int count = object1.Count;
                difference1.Object1Value = count.ToString((IFormatProvider)CultureInfo.InvariantCulture);
                count = object2.Count;
                difference1.Object2Value = count.ToString((IFormatProvider)CultureInfo.InvariantCulture);
                difference1.Object1 = (object)object1;
                difference1.Object2 = (object)object2;
                if (parms.Result.ExceededDifferences)
                    return true;
            }
            return object1[0] == object2[0] && object1[^1] == object2[^1];
        }
    }
}
