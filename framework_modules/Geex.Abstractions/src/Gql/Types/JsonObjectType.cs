using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;

namespace Geex.Abstractions.Gql.Types
{
    public class ObjectValueToJsonNodeConverter : SyntaxWalkerBase<IValueNode, Action<object>>
    {
        public JsonNode Convert(ObjectValueNode objectValue)
        {
            if (objectValue == null)
                throw new ArgumentNullException(nameof(objectValue));
            JsonNode dict = (JsonNode)null;
            Action<object> context = (Action<object>)(value => dict = (JsonNode)value);
            this.VisitObjectValue(objectValue, context);
            return dict;
        }

        public List<object> Convert(ListValueNode listValue)
        {
            if (listValue == null)
                throw new ArgumentNullException(nameof(listValue));
            List<object> list = (List<object>)null;
            Action<object> context = (Action<object>)(value => list = (List<object>)value);
            this.VisitListValue(listValue, context);
            return list;
        }

        protected override void VisitObjectValue(ObjectValueNode node, Action<object> setValue)
        {
            JsonNode obj = JsonNode.Parse(node.Fields.ToDictionary(x => x.Name.ToString(), x => x.Value.Value).ToJson());
            setValue((object)obj);
        }

        protected override void VisitListValue(ListValueNode node, Action<object> setValue)
        {
            List<object> list = new List<object>();
            setValue((object)list);
            Action<object> context = (Action<object>)(item => list.Add(item));
            foreach (IValueNode node1 in (IEnumerable<IValueNode>)node.Items)
                this.VisitValue(node1, context);
        }

        protected override void VisitIntValue(IntValueNode node, Action<object> setValue)
        {
            int result;
            if (int.TryParse(node.Value, NumberStyles.Integer, (IFormatProvider)CultureInfo.InvariantCulture, out result))
                setValue((object)result);
            else
                setValue((object)node.Value);
        }

        protected override void VisitFloatValue(FloatValueNode node, Action<object> setValue)
        {
            double result;
            if (double.TryParse(node.Value, NumberStyles.Float, (IFormatProvider)CultureInfo.InvariantCulture, out result))
                setValue((object)result);
            else
                setValue((object)node.Value);
        }

        protected override void VisitStringValue(StringValueNode node, Action<object> setValue) => setValue((object)node.Value);

        protected override void VisitBooleanValue(BooleanValueNode node, Action<object> setValue) => setValue((object)node.Value);

        protected override void VisitEnumValue(EnumValueNode node, Action<object> setValue) => setValue((object)node.Value);

        protected override void VisitNullValue(NullValueNode node, Action<object> setValue) => setValue((object)null);
    }
}
