using MediatR;

namespace Geex.Common.Abstraction.Auditing
{
    public class SubmitRequest<T> : IRequest<Unit>
    {
        public SubmitRequest(string? remark, string[] ids)
        {
            Remark = remark;
            this.Ids = ids;
        }

        public SubmitRequest(string? remark, string id)
        {
            Remark = remark;
            this.Ids = new[] { id };
        }

        public string? Remark { get; set; }
        public string[] Ids { get; set; }
    }
}