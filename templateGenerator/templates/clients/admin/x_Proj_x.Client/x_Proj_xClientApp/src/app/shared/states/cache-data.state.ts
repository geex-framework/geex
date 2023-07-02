import { Injectable, InjectionToken, Injector } from "@angular/core";
import { deepCopy } from "@delon/util";
import { State, Action, StateContext, StateToken, Selector, Store } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import { NzTreeNodeOptions } from "ng-zorro-antd/tree";
import { combineLatest, Observable, zip } from "rxjs";
import { debounceTime, delay, filter, map, take } from "rxjs/operators";

import {
  OrgCacheItemFragment,
  UserCacheDtoFragment,
  OrgsCacheGql,
  OrgsCacheQuery,
  OrgsCacheQueryVariables,
  CacheDataType,
  FrontendCallType,
  OnBroadcastGql,
  OnBroadcastSubscription,
  OnBroadcastSubscriptionVariables,
  AuditStatus,
  OrgBriefFragment,
  OrgTypeEnum,
} from "../graphql/.generated/type";
import { AppActions } from "./AppActions";
import { StateBase } from "./StateBase";
import { StateClassDefine } from "./common";
import { UserDataState } from "./user-data.state";

export interface CacheDataStateModel {
  cacheInit: boolean;
  orgs: OrgCacheItemFragment[];
  userOwnedOrg: OrgCacheItemFragment[];
}

// 定义依赖注入所需的token
export const CacheDataState$ = new InjectionToken<Observable<CacheDataStateModel>>("cacheData");
export type CacheDataState$ = Observable<CacheDataStateModel>;

@Injectable({ providedIn: "root" })
@StateClassDefine<CacheDataStateModel>({
  cacheInit: false,
  orgs: [],
  userOwnedOrg: [],
})
export class CacheDataState extends StateBase<CacheDataStateModel> {
  @Selector()
  static orgs(state: CacheDataStateModel) {
    return state.orgs;
  }
  async init(forceReload?: boolean) {
    const client = this.apollo.use("subscription");
    client
      .subscribe<OnBroadcastSubscription, OnBroadcastSubscriptionVariables>({
        query: OnBroadcastGql,
      })
      .pipe(
        filter(x => x.data.onBroadcast.frontendCallType == FrontendCallType.CacheDataChange),
        map(x => x.data.onBroadcast.data as { type: CacheDataType }),
        debounceTime(1000),
      )
      .subscribe(async x => {
        await this.store.dispatch(new AppActions.CacheDataUpdated(x.type)).toPromise();
      });
    // if (forceReload) {
    //   for (const [key, value] of Object.entries(CacheDataType)) {
    //     tasks.add(this.store.dispatch(new AppAction.CacheDataUpdated(value)).toPromise());
    //   }
    // }
    await this.store.dispatch(new AppActions.LoadCacheData()).toPromise();
  }
  orgs$: Observable<OrgCacheItemFragment[]>;
  userOwnedOrg$: Observable<OrgCacheItemFragment[]>;
  /**
   *
   */
  constructor(private apollo: Apollo, injector: Injector) {
    super(injector);
    const client = apollo.use("silent");
    this.orgs$ = client
      .query<OrgsCacheQuery, OrgsCacheQueryVariables>({
        query: OrgsCacheGql,
        variables: {},
      })
      .pipe(map(x => x.data.orgsCache));
    this.userOwnedOrg$ = combineLatest([
      this.orgs$,
      this.store.select(UserDataState).pipe(
        filter(x => x?.id != undefined),
        take(1),
      ),
    ]).pipe(
      map(([orgs, userData]) => {
        let allOwnedOrgs: OrgCacheItemFragment[] = [];
        //超级管理员忽视过滤
        if (userData.id == "000000000000000000000001") {
          allOwnedOrgs = deepCopy(orgs);
        } else {
          // 找到所有父组织
          let ownedOrgCodes = userData.orgs.map(x => x.code);
          // 找到所有开头和父组织名匹配的组织(包含祖先组织)
          allOwnedOrgs = orgs.where(x => ownedOrgCodes.any(y => y.startsWith(x.code)));
        }
        return allOwnedOrgs;
      }),
    );
  }

  @Action(AppActions.LoadCacheData)
  async onLoadCacheData(ctx: StateContext<CacheDataStateModel>, action: AppActions.LoadCacheData) {
    let tasks = await Promise.all([this.orgs$.toPromise(), this.userOwnedOrg$.toPromise()]);
    let [orgs, userOwnedOrg] = tasks;
    try {
      Object.defineProperty(window, "orgs", {
        get: () => ctx.getState().orgs,
      });
      Object.defineProperty(window, "userOwnedOrg", {
        get: () => ctx.getState().userOwnedOrg,
      });
    } catch {}
    ctx.patchState({
      cacheInit: true,
      orgs: orgs,
      userOwnedOrg: userOwnedOrg,
    });
    return tasks;
  }

  @Action(AppActions.CacheDataUpdated)
  async onCacheDataUpdated(ctx: StateContext<CacheDataStateModel>, action: AppActions.CacheDataUpdated) {
    switch (action.type) {
      case CacheDataType.Org:
        let orgs = await this.orgs$.toPromise();
        let userOwnedOrg = await this.userOwnedOrg$.toPromise();
        ctx.patchState({
          orgs,
          userOwnedOrg,
        });
        break;
      default:
        break;
    }
  }
}
