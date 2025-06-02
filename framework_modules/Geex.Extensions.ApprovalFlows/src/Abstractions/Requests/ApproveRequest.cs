using MediatR;

namespace Geex.Extensions.ApprovalFlows.Requests;

public class ApproveRequest<T> : IRequest
{
    public string? Remark { get; set; }

    public ApproveRequest(string? remark, string[] ids)
    {
        Remark = remark;
        this.Ids = ids;
    }
    public ApproveRequest(string? remark, string id)
    {
        Remark = remark;
        this.Ids = new[] { id };
    }

    public string[] Ids { get; set; }
}