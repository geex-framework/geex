import { Component, Injector } from "@angular/core";
import * as _ from "lodash-es";

import type { ITenant } from "@geexcode/geex-extensions-identity";
import { tenants, toggleTenantAvailability } from "../graphql/operations.gql";
import { TenantEditComponent } from "../components/tenant-edit/tenant-edit.component";
import { RoutedListComponent } from "@geexcode/geex-angular";
import { RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import { STColumn } from "@delon/abc/st";

type TenantListParams = {
  pi: number;
  ps: number;
  filter: string;
};

@Component({
  templateUrl: "./tenant-list.page.html",
  styles: [],
  standalone: true,
  imports: [SharedModule],
})
export class TenantListComponent extends RoutedListComponent<TenantListParams, ITenant> {
  override columns?: Array<STColumn<ITenant>>;
  routeParamsMappings: RouteParamsMappings<TenantListParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
    filter: { position: "queryParams", default: "" },
  };

  async onRouted(params: TenantListParams) {
    let res = await this.apollo
      .query({
        query: tenants,
        variables: {
          skip: (params.pi - 1) * params.ps,
          take: params.ps,
          filter: {
            or: [{ code: { contains: params.filter } }, { name: { contains: params.filter } }],
          },
        },
      })
      .firstValuePromise();
    this.selectedData.set([]);
    this.data.set(res.data.tenants.items);
    this.total.set(res.data.tenants.totalCount);
  }

  filter: string;
  async add() {
    let changed: boolean = await this.modal.create(TenantEditComponent, {}).firstValuePromise();
    if (changed) {
      this.refresh();
    }
  }
  // async refresh() {
  //   let params = this.params();
  //   await this.router.navigate([], { queryParams: { pi: params.pi, ps: params.ps, filter: this.filter } });
  // }
  async toggleAvailability(code: string) {
    let res = await this.apollo
      .mutate({
        mutation: toggleTenantAvailability,
        variables: {
          code: code,
        },
      })
      .firstValuePromise();
    this.msgSrv.success("操作成功");
    await this.refresh();
  }
  async edit(tenant: Hint<ITenant>) {
    let changed: boolean = await this.modal.create(TenantEditComponent, { code: tenant.code, name: tenant.name }).firstValuePromise();
    if (changed) {
      await this.refresh();
    }
  }
}
