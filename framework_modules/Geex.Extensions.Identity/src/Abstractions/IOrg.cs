using System.Collections.Generic;
using System.Linq;
using Geex.MultiTenant;

namespace Geex.Extensions.Identity
{
    public interface IOrg:ITenantFilteredEntity
    {
        /// <summary>
        ///     所有父组织编码
        /// </summary>
        List<string> AllParentOrgCodes { get; }

        /// <summary>
        ///     所有父组织
        /// </summary>
        //IQueryable<IOrg> AllParentOrgs { get; }

        /// <summary>
        ///     所有子组织编码
        /// </summary>
        List<string> AllSubOrgCodes { get; }

        /// <summary>
        ///     所有子组织
        /// </summary>
        //IQueryable<Org> AllSubOrgs { get; }

        /// <summary>
        ///     直系子组织编码
        /// </summary>
        List<string> DirectSubOrgCodes { get; }

        /// <summary>
        ///     直系子组织
        /// </summary>
        //IQueryable<Org> DirectSubOrgs { get; }

        /// <summary>
        ///     父组织
        /// </summary>
        //Org ParentOrg { get; }

        /// <summary>
        ///     父组织编码
        /// </summary>
        string ParentOrgCode { get; }

        /// <summary>
        ///     以.作为分割线的编码
        /// </summary>
        string Code { get; set; }

        string Name { get; set; }

        /// <summary>
        ///   组织类型  
        /// </summary>
        OrgTypeEnum OrgType { get; set; }

        /// <summary>
        ///     所有父组织
        /// </summary>
        IQueryable<IOrg> AllParentOrgs { get; }

        /// <summary>
        ///     所有子组织
        /// </summary>
        IQueryable<IOrg> AllSubOrgs { get; }

        /// <summary>
        ///     直系子组织
        /// </summary>
        IEnumerable<IOrg> DirectSubOrgs { get; }

        /// <summary>
        ///     父组织
        /// </summary>
        IOrg ParentOrg { get; }

        /// <summary>
        ///     修改组织编码
        /// </summary>
        /// <param name="newOrgCode"></param>
        void SetCode(string newOrgCode);

        /// <summary>
        ///     更新组织信息
        /// </summary>
        /// <param name="name">组织名称</param>
        /// <param name="code">组织编码</param>
        /// <param name="orgType">组织类型</param>
        void UpdateOrg(string? name = null, string? code = null, OrgTypeEnum? orgType = null);

        /// <summary>
        ///     移动组织到新的父级
        /// </summary>
        /// <param name="newParentOrgCode">新的父组织编码</param>
        void MoveToParent(string newParentOrgCode);
    }
}
