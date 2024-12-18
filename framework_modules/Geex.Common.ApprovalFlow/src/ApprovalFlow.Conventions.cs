﻿using Geex.Common.Abstraction;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.ApprovalFlows;

public partial class ApprovalFlow
{
    public class ApprovalFlowBsonConfig : BsonConfig<ApprovalFlow>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<ApprovalFlow> map, BsonIndexConfig<ApprovalFlow> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.CreatorUserId));
        }
    }

    public class ApprovalFlowGqlConfig : GqlConfig.Object<ApprovalFlow>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApprovalFlow> descriptor)
        {
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
