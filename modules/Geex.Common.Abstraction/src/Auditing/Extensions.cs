using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Auditing
{
    public static class AuditExtensions
    {
        public static void CheckAuditEntityEditable(this IAuditEntity entity)
        {

            if (entity.AuditStatus != AuditStatus.Default)
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: "已上报或审批的对象不允许进行此操作");
            }
        }
    }
}
