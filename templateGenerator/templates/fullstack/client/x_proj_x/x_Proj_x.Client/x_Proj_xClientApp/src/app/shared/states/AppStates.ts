import { CacheDataState as CacheDataStateDefine } from "./cache-data.state";
import { TenantState as TenantStateDefine } from "./tenant.state";
import { UserDataState as UserDataStateDefine } from "./user-data.state";

export namespace AppStates {
  export const TenantState = TenantStateDefine;
  export const UserDataState = UserDataStateDefine;
  export const CacheDataState = CacheDataStateDefine;
}
