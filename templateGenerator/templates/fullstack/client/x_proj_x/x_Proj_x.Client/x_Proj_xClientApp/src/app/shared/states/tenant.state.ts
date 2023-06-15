import { Injectable, Injector } from "@angular/core";
import { deepCopy } from "@delon/util";
import { Action, StateContext } from "@ngxs/store";
import { Apollo, ApolloBase } from "apollo-angular";
import { map } from "rxjs/operators";

import {
  CheckTenantGql,
  CheckTenantMutation,
  CheckTenantMutationVariables,
  ITenant,
  TenantsGql,
  TenantsQuery,
  TenantsQueryVariables,
} from "../graphql/.generated/type";
import { AppActions } from "./AppActions";
import { StateBase } from "./StateBase";
import { StateClassDefine } from "./common";

export type TenantStateModel = Partial<ITenant>;

@Injectable({ providedIn: "root" })
@StateClassDefine<TenantStateModel>({ code: "" })
export class TenantState extends StateBase<TenantStateModel> {
  client: ApolloBase;
  /**
   *
   */
  constructor(injector: Injector) {
    super(injector);
    this.client = this.injector.get(Apollo).use("silent");
  }

  @Action(AppActions.TenantChanged)
  async onTenantDetermined(ctx: StateContext<TenantStateModel>, action: AppActions.TenantChanged) {
    if (action.tenantCode == ctx.getState().code) {
      return;
    }
    let tenant = await this.getTenantInfoByCode(action.tenantCode);
    if (tenant == null) {
      alert(`租户[${action.tenantCode}]不存在或未启用`);
      throw new Error(`租户[${action.tenantCode}]不存在或未启用`);
    }
    ctx.setState(tenant);
  }
  async getTenantInfoByCode(tenantCode: string) {
    let host = { code: null, name: "__host" };
    if (tenantCode && tenantCode.length > 0) {
      const userRes = await this.client
        .mutate<CheckTenantMutation, CheckTenantMutationVariables>({
          mutation: CheckTenantGql,
          variables: {
            code: tenantCode,
          },
        })
        .toPromise();

      let tenant = userRes?.data?.checkTenant;
      if (tenant == null) {
        return host;
      }
      return tenant;
    }
    return host;
  }
}
