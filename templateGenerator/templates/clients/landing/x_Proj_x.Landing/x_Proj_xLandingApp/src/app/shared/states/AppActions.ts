import { OrgCacheItemFragment, CacheDataType } from "../graphql/.generated/type";
import { UserDataStateModel } from "./user-data.state";

export namespace AppActions {
  export class TenantChanged {
    static readonly type = "[Tenant] TenantChanged";
    constructor(public tenantCode?: string) {}
  }

  export class LoadCacheData {
    static readonly type = "[CacheData] LoadCacheData";
    constructor() {}
  }

  export class UserDataLoaded {
    static readonly type = "[UserData] LoadUserData";
    constructor(public data: UserDataStateModel) {}
  }

  export class CacheDataUpdated {
    static readonly type = "[CacheData] CacheDataUpdated";
    data: OrgCacheItemFragment[];
    constructor(public type: CacheDataType) {}
  }
}
export type AppActions = InstanceType<typeof AppActions[keyof typeof AppActions]>;
