using System.Text.Json.Nodes;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.AuditLogs.Enums;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.AuditLogs
{
    public partial class AuditLog : Entity<AuditLog>, ITenantFilteredEntity
    {
        public OperationType OperationType { get; set; }
        public string? OperatorId { get; set; }
        public string? Query { get; set; }
        public JsonNode? Result { get; set; }
        public bool IsSuccess { get; set; }

        /// <inheritdoc />
        public string? TenantCode { get; set; }
    }

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
            }
        }

        public class AuditLogGqlConfig : GqlConfig.Object<AuditLog>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<AuditLog> descriptor)
            {
                descriptor.ConfigEntity();
                descriptor.Field(x=>x.OperatorId);
                descriptor.Field(x=>x.OperationType);
                descriptor.Field(x=>x.Query);
                descriptor.Field(x=>x.IsSuccess);
                descriptor.Field(x=>x.Result);
                base.Configure(descriptor);
            }
        }
    }

}
