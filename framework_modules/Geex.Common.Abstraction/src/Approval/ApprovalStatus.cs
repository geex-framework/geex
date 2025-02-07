using System;

namespace Geex.Common.Abstraction.Approval
{
    public enum ApproveStatus
    {
        /// <summary>
        /// 待上报/默认
        /// </summary>
        Default = 0b0,
        /// <summary>
        /// 已上报
        /// </summary>
        Submitted = 0b1,
        /// <summary>
        /// 已审批
        /// </summary>
        Approved = 0b11,

    }
}
