using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Approbation
{
    public static class ApproveExtensions
    {
        public static void CheckApproveEntityEditable(this IApproveEntity entity)
        {

            if (entity.ApproveStatus != ApproveStatus.Default)
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: "已上报或审批的对象不允许进行此操作");
            }
        }
    }
}
