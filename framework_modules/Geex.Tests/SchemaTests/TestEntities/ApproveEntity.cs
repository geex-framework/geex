using Geex.ApprovalFlows;
using Geex.Storage;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public class ApproveEntity : Entity<ApproveEntity>, IApproveEntity
    {
        /// <inheritdoc />
        public ApproveStatus ApproveStatus { get; set; }

        /// <inheritdoc />
        public string? ApproveRemark { get; set; }

        /// <inheritdoc />
        public bool Submittable { get; }
    }
}
