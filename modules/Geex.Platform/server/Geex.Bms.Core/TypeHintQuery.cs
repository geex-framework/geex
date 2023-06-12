using Geex.Common.Abstraction.Gql.Types;
using HotChocolate.Types;

namespace Geex.Bms.Core
{
     public class HintQuery : QueryExtension<HintQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<HintQuery> descriptor)
        {
            base.Configure(descriptor);
            descriptor.Field("_hint").Type<ObjectType<HintType>>().Resolve(x => null);
        }

        public class HintType
        {
            // here to put your hint types
            //public DataDisplayUnit DataDisplayUnit { get; set; }
            public string _ { get; set; }
        }
    }
}
