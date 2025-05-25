using System.Text.Json.Nodes;
using Geex.Abstractions;
using Geex.Extensions.AuditLogs.Enums;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Execution;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.AuditLogs
{
    public partial class AuditLog : Entity<AuditLog>, ITenantFilteredEntity
    {
        public OperationType OperationType { get; set; }
        public string OperationName { get; set; }
        public string? Operation { get; set; }
        public JsonNode? Variables { get; set; }
        public JsonNode? Result { get; set; }
        public bool IsSuccess { get; set; }
        public string? OperatorId { get; set; }

        /// <inheritdoc />
        public string? TenantCode { get; set; }

        public string? ClientIp { get; set; }
    }
}
