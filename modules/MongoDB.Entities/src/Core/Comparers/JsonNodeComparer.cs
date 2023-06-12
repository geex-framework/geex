using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

namespace MongoDB.Entities.Core.Comparers
{
    public class JsonNodeComparer : BaseTypeComparer
    {
        /// <inheritdoc />
        public JsonNodeComparer(RootComparer rootComparer) : base(rootComparer)
        {
        }

        /// <inheritdoc />
        public override bool IsTypeMatch(Type type1, Type type2)
        {
            if (type1?.IsAssignableTo<JsonNode>() == true && type2?.IsAssignableTo<JsonNode>() == true)
            {
                return type1 == type2;
            }
            return false;
        }

        /// <inheritdoc />
        public override void CompareType(CompareParms parms)
        {
            string v1 = parms.Object1.ToString();
            if (parms.Object1 is JsonNode e1)
            {
                v1 = e1.ToJsonString();
            }
            string v2 = parms.Object2.ToString();
            if (parms.Object2 is JsonNode e2)
            {
                v2 = e2.ToJsonString();
            }

            if (v1 == v2)
            {
                return;
            }

            AddDifference(parms);
        }
    }
}
