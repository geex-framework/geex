using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Entities;
using Geex.Abstractions.MultiTenant;
using Geex.Abstractions.Storage;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;

using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.Aggregates.Orgs;

public class Org : Entity<Org>, ITenantFilteredEntity, IOrg
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

    private IEnumerable<IOrg> _directSubOrgsQuery =>
        DbContext.Find<Org>().Match(builder => builder.Regex(x=>x.Code, BsonRegularExpression.Create($@"^{Code}\.\w+(?!\.)$"))).ExecuteAsync().GetAwaiter().GetResult();

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
        foreach (var subOrg in subOrgs) subOrg.SetCode(subOrg.Code.Replace(Code, newOrgCode));

    }

    /// <inheritdoc />
    public override Task<long> DeleteAsync()
    {
        return base.DeleteAsync();
    }

    public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }

    public class OrgBsonConfig : BsonConfig<Org>
    {
        protected override void Map(BsonClassMap<Org> map, BsonIndexConfig<Org> indexConfig)
        {
            map.Inherit<IOrg>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x=>x.Ascending(y=>y.Code), options => options.Background = true);
            indexConfig.MapIndex(x=>x.Ascending(y=>y.Name), options => options.Background = true);
            indexConfig.MapIndex(x=>x.Ascending(y=>y.OrgType), options => options.Background = true);
        }
    }
    public class OrgGqlConfig : GqlConfig.Object<Org>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Org> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.Users).Type<ListType<UserType>>().Resolve(x=>x.ToString());
            //descriptor.Field(x => x.Code);
            //descriptor.Field(x => x.Name);
            //descriptor.Field(x => x.OrgType);
            //descriptor.Field(x => x.AllSubOrgCodes);
            //descriptor.Field(x => x.DirectSubOrgCodes);
            //descriptor.Field(x => x.AllSubOrgs);
            //descriptor.Field(x => x.DirectSubOrgs);
            //descriptor.Field(x => x.ParentOrgCode);
            //descriptor.Field(x => x.ParentOrg);
            //descriptor.Field(x => x.AllParentOrgCodes);
            //descriptor.Field(x => x.AllParentOrgs);
        }
    }
}
