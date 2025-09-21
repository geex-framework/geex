using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.MultiTenant;
using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class Org : Entity<Org>, ITenantFilteredEntity, IOrg
{
    public Org()
    {
    }

    public Org(string code, string name, OrgTypeEnum orgTypeEnum = default)
    {
        Code = code;
        Name = name;
        OrgType = orgTypeEnum ?? OrgTypeEnum.Default;
    }

    private IQueryable<Org> _allSubOrgsQuery => DbContext.Query<Org>().Where(x => x.Code.StartsWith(Code + "."));

    private IEnumerable<IOrg> _directSubOrgsQuery => _allSubOrgsQuery.AsEnumerable().Where(x => x.Code.Count(y => y == '.') == this.Code.Count(y => y == '.') + 1);

    /// <summary>
    ///     所有父组织编码
    /// </summary>
    public List<string> AllParentOrgCodes => ParentOrgCode.Split('.', StringSplitOptions.RemoveEmptyEntries).Aggregate(
        new List<string>(), (list, next) => list.Append(
            list.LastOrDefault().IsNullOrEmpty() ? next : string.Join('.', list.LastOrDefault(), next)).ToList());

    /// <summary>
    ///     所有父组织
    /// </summary>
    public IQueryable<IOrg> AllParentOrgs => DbContext.Query<Org>().Where(x => AllParentOrgCodes.Contains(x.Code));

    /// <summary>
    ///     所有子组织编码
    /// </summary>
    public List<string> AllSubOrgCodes => _allSubOrgsQuery.Select(x => x.Code).ToList();

    /// <summary>
    ///     所有子组织
    /// </summary>
    public IQueryable<IOrg> AllSubOrgs => _allSubOrgsQuery;

    /// <summary>
    ///     直系子组织编码
    /// </summary>
    public List<string> DirectSubOrgCodes => _directSubOrgsQuery.Select(x => x.Code).ToList();

    /// <summary>
    ///     直系子组织
    /// </summary>
    public IEnumerable<IOrg> DirectSubOrgs => _directSubOrgsQuery;

    /// <summary>
    ///     父组织
    /// </summary>
    public IOrg ParentOrg => DbContext.Query<Org>().FirstOrDefault(x => ParentOrgCode == x.Code);

    /// <summary>
    ///     父组织编码
    /// </summary>
    public string ParentOrgCode => Code.Split('.').SkipLast(1).JoinAsString(".");

    /// <summary>
    ///     编码
    /// </summary>
    public string Code { get; set; }

    public string Name { get; set; }

    /// <summary>
    ///     组织类型
    /// </summary>
    public OrgTypeEnum OrgType { get; set; }

    /// <inheritdoc />
    public string? TenantCode { get; set; }

    /// <summary>
    ///     修改组织编码
    /// </summary>
    /// <param name="newOrgCode"></param>
    public void SetCode(string newOrgCode)
    {
        var originCode = Code;

        var subOrgs = DirectSubOrgs.ToList();
        this.Code = newOrgCode;
        foreach (var subOrg in subOrgs) subOrg.SetCode(subOrg.Code.Replace(originCode, newOrgCode));
    }

    /// <param name="cancellation"></param>
    /// <inheritdoc />
    public override async Task<long> DeleteAsync(CancellationToken cancellation = default)
    {
        if (this.DbContext.Query<User>().Any(x => x.OrgCodes.Contains(this.Code)))
        {
            throw new BusinessException($"组织{this.ToString()}中存在用户，不能删除, 请先移除用户");
        }
        long result = 0;
        var subs = this.DirectSubOrgs.ToList();
        foreach (var sub in subs)
        {
            result += await sub.DeleteAsync(cancellation);
        }

        result += await base.DeleteAsync(cancellation);
        return result;
    }

    /// <summary>
    ///     更新组织信息
    /// </summary>
    /// <param name="name">组织名称</param>
    /// <param name="code">组织编码</param>
    /// <param name="orgType">组织类型</param>
    public void UpdateOrg(string? name = null, string? code = null, OrgTypeEnum? orgType = null)
    {
        if (!name.IsNullOrEmpty())
            Name = name;

        if (!code.IsNullOrEmpty() && code != Code)
            SetCode(code);

        if (orgType != null)
            OrgType = orgType;
    }

    /// <summary>
    ///     移动组织到新的父级
    /// </summary>
    /// <param name="newParentOrgCode">新的父组织编码</param>
    public void MoveToParent(string newParentOrgCode)
    {
        var currentCode = Code;
        var parentPrefix = ParentOrgCode;

        // 构建新的编码
        var newCode = newParentOrgCode.IsNullOrEmpty()
            ? currentCode.Split('.').LastOrDefault()
            : $"{newParentOrgCode}.{currentCode.Split('.').LastOrDefault()}";

        SetCode(newCode);
    }

    public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }

    public override string ToString()
    {
        return $"{Name}[{Code}]";
    }
}
