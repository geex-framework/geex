using Geex.Common.Abstraction;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.AuditLogs;

public partial class AuditLog
{
    public class AuditLogBsonConfig : BsonConfig<AuditLog>
    {
        /// <inheritdoc />
        protected override void Map(BsonClassMap<AuditLog> map, BsonIndexConfig<AuditLog> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Descending(o => o.OperatorId));
            indexConfig.MapIndex(x => x.Hashed(o => o.IsSuccess));
            indexConfig.MapIndex(x => x.Hashed(o => o.OperationType));
            indexConfig.MapIndex(x => x.Hashed(o => o.OperationName));
        }
    }

    public class AuditLogGqlConfig : GqlConfig.Object<AuditLog>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuditLog> descriptor)
        {
            descriptor.ConfigEntity();
            descriptor.Field(x => x.OperatorId);
            descriptor.Field(x => x.OperationType);
            descriptor.Field(x => x.OperationName);
            descriptor.Field(x => x.Operation);
            descriptor.Field(x => x.IsSuccess);
            descriptor.Field(x => x.Result);
            base.Configure(descriptor);
        }
    }
}