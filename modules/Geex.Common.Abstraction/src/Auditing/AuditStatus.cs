using System;

namespace Geex.Common.Abstraction.Auditing
{
    [Flags]
    public enum AuditStatus
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
        Audited = 3,

    }
}
