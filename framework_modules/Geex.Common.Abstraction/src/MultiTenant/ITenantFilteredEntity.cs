﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using MongoDB.Entities;
using MongoDB.Entities.Interceptors;

namespace Geex.Common.Abstraction.MultiTenant
{
    /// <summary>
    /// 租户过滤标记接口, 被标记的实体将默认参与租户过滤
    /// </summary>
    public interface ITenantFilteredEntity : IIntercepted, IEntityBase
    {
        /// <summary>
        /// 租户编码, 为null时为宿主数据
        /// </summary>
        public string? TenantCode { get; [Obsolete(message: "框架会自动维护租户编码, 请勿直接set.", error: true)] set; }

        /// <summary>
        /// 设置租户信息<br/>
        /// <remarks>不会修改租户信息</remarks>
        /// </summary>
        /// <param name="code"></param>
        [Obsolete("geex 会自动处理租户编码, 绝大多数情况不需要手动设置租户信息")]
        public void SetTenant(string? code)
        {
            var property = this.GetType().GetProperty(nameof(TenantCode));
            if (property == null || property.CanWrite == false)
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: $"{nameof(ITenantFilteredEntity)}.{nameof(TenantCode)}必须拥有直接的setter.");
            }

            if (property.GetValue(this) == null)
            {
                property.SetValue(this, code);
            }
        }
    }
}
