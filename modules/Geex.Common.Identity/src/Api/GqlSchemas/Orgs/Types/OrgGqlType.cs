using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using HotChocolate.Types;

namespace Geex.Common.Identity.Api.GqlSchemas.Orgs.Types
{
    public class OrgGqlType : ObjectType<Org>
    {
        protected override void Configure(IObjectTypeDescriptor<Org> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsExplicitly();
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.Users).Type<ListType<UserType>>().Resolve(x=>x.ToString());
            descriptor.Field(x => x.Code);
            descriptor.Field(x => x.Name);
            descriptor.Field(x => x.OrgType);
            descriptor.Field(x => x.AllSubOrgCodes);
            descriptor.Field(x => x.DirectSubOrgCodes);
            descriptor.Field(x => x.AllSubOrgs);
            descriptor.Field(x => x.DirectSubOrgs);
            descriptor.Field(x => x.ParentOrgCode);
            descriptor.Field(x => x.ParentOrg);
            descriptor.Field(x => x.AllParentOrgCodes);
            descriptor.Field(x => x.AllParentOrgs);
            base.Configure(descriptor);
        }
    }
}
