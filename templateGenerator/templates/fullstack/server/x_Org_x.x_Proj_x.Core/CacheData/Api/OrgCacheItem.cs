﻿using System.Collections.Generic;
using System.Linq;
using Geex.Common.Abstraction.Entities;

namespace x_Org_x.x_Proj_x.Core.CacheData.Api;

public record OrgCacheItem(OrgTypeEnum? OrgType, string? Code, string? Name)
{
    public string ParentOrgCode => this.Code.Split('.').SkipLast(1).JoinAsString(".");
}
