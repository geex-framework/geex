using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Amazon.Runtime.Internal.Util;

using Geex.Abstractions;
using Geex.ApprovalFlows;
using Geex.Storage;

using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.ApprovalFlows;

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