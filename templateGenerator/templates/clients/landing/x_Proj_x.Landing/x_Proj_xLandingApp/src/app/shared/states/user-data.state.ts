import { Injectable, InjectionToken, Injector } from "@angular/core";
import { State, Action, StateContext, StateToken, Selector } from "@ngxs/store";
import { Apollo } from "apollo-angular";
import { Observable } from "rxjs";
import { debounceTime, filter, map } from "rxjs/operators";

import {
  FrontendCallType,
  OnFrontendCallGql,
  OnFrontendCallSubscription,
  OnFrontendCallSubscriptionVariables,
  UserDetailFragment,
} from "../graphql/.generated/type";
import { AppActions } from "./AppActions";
import { StateBase } from "./StateBase";
import { StateClassDefine } from "./common";

export type UserDataStateModel = Partial<UserDetailFragment>;

// 定义依赖注入所需的token
export const UserDataState$ = new InjectionToken<Observable<UserDataStateModel>>("userData");
export type UserDataState$ = Observable<UserDataStateModel>;

@Injectable({ providedIn: "root" })
@StateClassDefine<UserDataStateModel>({})
export class UserDataState extends StateBase<UserDataStateModel> {
  /**
   *
   */
  constructor(private apollo: Apollo, injector: Injector) {
    super(injector);
  }
  async init() {
    this.apollo
      .use("subscription")
      .subscribe<OnFrontendCallSubscription, OnFrontendCallSubscriptionVariables>({
        query: OnFrontendCallGql,
      })
      .pipe(
        filter(x => x.data.onFrontendCall.frontendCallType == FrontendCallType.CacheDataChange),
        map(x => x.data.onFrontendCall.data),
        debounceTime(1000),
      )
      .subscribe(async x => {
        //console.log(x);
      });
  }
  @Selector()
  static orgs(state: UserDataStateModel) {
    return state.orgs;
  }
  @Action(AppActions.UserDataLoaded)
  onUserDataLoaded(ctx: StateContext<UserDataStateModel>, action: AppActions.UserDataLoaded) {
    ctx.patchState(action.data);
  }
}
