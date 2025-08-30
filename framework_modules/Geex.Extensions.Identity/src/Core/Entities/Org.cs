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
    public override Task<long> DeleteAsync(CancellationToken cancellation = default)
    {
        return base.DeleteAsync(cancellation);
    }

    public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }
}
