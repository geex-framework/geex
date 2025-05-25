using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Abstractions.Approval;
using Geex.Abstractions.Storage;

namespace Geex.Tests.TestEntities
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
