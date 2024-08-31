using System;

namespace Geex.Common.Abstraction.Approbation
{
    [Flags]
    public enum ApproveStatus
    {
        /// <summary>
        /// 待上报/默认
        /// </summary>
        Default = 0,
        /// <summary>
        /// 已上报
        /// </summary>
        Submitted = 1,
        /// <summary>
        /// 已审批
        /// </summary>
        Approved = 3,

    }
}
