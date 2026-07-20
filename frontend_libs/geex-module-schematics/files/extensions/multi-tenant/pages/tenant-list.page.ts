import { Component, computed } from "@angular/core";

import type { ITenant } from "@geexcode/geex-extensions-identity";
import { tenants, toggleTenantAvailability } from "../graphql/operations.gql";
import { TenantEditComponent } from "../components/tenant-edit/tenant-edit.component";
import { RoutedListComponent, RouteParamsMappings } from "@geexcode/geex-angular";
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
  override columns = computed<STColumn<ITenant>[]>(() => [
    { title: this.I18N.Tenant.list.columnCode, index: "code" },
    { title: this.I18N.Tenant.list.columnName, index: "name" },
    {
      title: this.I18N.Common.list.actions,
      buttons: [
        { text: this.I18N.Common.action.edit, click: item => this.edit(item) },
        {
          text: this.I18N.Common.action.disable,
          iif: item => !!item.isEnabled,
          click: item => this.toggleAvailability(item.code),
        },
        {
          text: this.I18N.Common.action.enable,
          iif: item => !item.isEnabled,
          click: item => this.toggleAvailability(item.code),
        },
      ],
    },
  ]);

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

  async add() {
    let changed: boolean = await this.modal.create(TenantEditComponent, {}).firstValuePromise();
    if (changed) {
      this.refresh();
    }
  }

  async toggleAvailability(code: string) {
    await this.apollo
      .mutate({
        mutation: toggleTenantAvailability,
        variables: {
          code: code,
        },
      })
      .firstValuePromise();
    this.msgSrv.success(this.I18N.Common.message.operationSuccess);
    await this.refresh();
  }

  async edit(tenant: Hint<ITenant>) {
    let changed: boolean = await this.modal.create(TenantEditComponent, { code: tenant.code, name: tenant.name }).firstValuePromise();
    if (changed) {
      await this.refresh();
    }
  }
}
