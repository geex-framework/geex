import { Component, Injector, OnInit } from "@angular/core";
import { deepCopy } from "@delon/util";
import { Apollo } from "apollo-angular";
import _ from "lodash";
import { take } from "rxjs/operators";

import { BusinessComponentBase } from "../../../shared/components";
import {
  AppPermission,
  EditTenantGql,
  ITenant,
  TenantsGql,
  TenantsQuery,
  TenantsQueryVariables,
  ToggleTenantAvailabilityGql,
} from "../../../shared/graphql/.generated/type";
import { TenantEditComponent } from "../components/tenant-edit/tenant-edit.component";

@Component({
  templateUrl: "./list.component.html",
  styles: [],
})
export class TenantListComponent extends BusinessComponentBase {
  async prepare(params: any) {
    params = _.merge({ pi: 1, ps: 10, filter: "" }, params);
    let data = await this.apollo
      .query<TenantsQuery, TenantsQueryVariables>({
        query: TenantsGql,
        variables: {
          skip: (params.pi - 1) * params.ps,
          take: params.ps,
          where: {
            or: [{ code: { contains: params.filter } }, { name: { contains: params.filter } }],
          },
        },
      })
      .toPromise();
    this.data = deepCopy(data.data.tenants.items);
  }
  AppPermission = AppPermission;
  data = [];
  constructor(injector: Injector) {
    super(injector);
  }

  filter: string;
  async add() {
    let changed: boolean = await this.modal.create(TenantEditComponent, {}).toPromise();
    if (changed) {
      await this.refresh();
    }
  }
  async refresh() {
    let params = await this.$params.pipe(take(1)).toPromise();
    await this.router.navigate([], { queryParams: { pi: params.pi, ps: params.ps, filter: this.filter } });
  }
  async toggleAvailability(code: string) {
    let res = await this.apollo
      .mutate({
        mutation: ToggleTenantAvailabilityGql,
        variables: {
          code: code,
        },
      })
      .toPromise();
    this.msgSrv.success("操作成功");
    await this.refresh();
  }
  async edit(tenant: ITenant) {
    let changed: boolean = await this.modal.create(TenantEditComponent, { code: tenant.code, name: tenant.name }).toPromise();
    if (changed) {
      await this.refresh();
    }
  }
}
