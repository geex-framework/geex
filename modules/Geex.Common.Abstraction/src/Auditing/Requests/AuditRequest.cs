using MediatR;

namespace Geex.Common.Abstraction.Auditing
{
    public class AuditRequest<T> : IRequest<Unit>
    {
        public string? Remark { get; set; }

        public AuditRequest(string? remark, string[] ids)
        {
            Remark = remark;
            this.Ids = ids;
        }
        public AuditRequest(string? remark, string id)
        {
            Remark = remark;
            this.Ids = new[] { id };
        }

        public string[] Ids { get; set; }
    }
}