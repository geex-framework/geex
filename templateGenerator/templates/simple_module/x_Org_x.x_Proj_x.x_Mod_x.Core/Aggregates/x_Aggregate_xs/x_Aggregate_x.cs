using System;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Storage;

using HotChocolate.Types;

using MongoDB.Bson.Serialization;

namespace x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public class x_Aggregate_x : Entity<x_Aggregate_x>, IAuditEntity
    {
        public x_Aggregate_x(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        /// <inheritdoc />
        public AuditStatus AuditStatus { get; set; }

        /// <inheritdoc />
        public string? AuditRemark { get; set; }

        /// <inheritdoc />
        public bool Submittable { get; }

         public class x_Aggregate_xMapConfig : BsonConfig<x_Aggregate_x>
        {
            /// <inheritdoc />
            protected override void Map(BsonClassMap<x_Aggregate_x> map, BsonIndexConfig<x_Aggregate_x> indexConfig)
            {
                map.SetIsRootClass(true);
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
            }
        }

        public class x_Aggregate_xGqlType : GqlConfig.Object<x_Aggregate_x>
        {
            protected override void Configure(IObjectTypeDescriptor<x_Aggregate_x> descriptor)
            {
                // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
                descriptor.BindFieldsImplicitly();
                descriptor.ConfigEntity();
                base.Configure(descriptor);
            }
        }
    }
}
