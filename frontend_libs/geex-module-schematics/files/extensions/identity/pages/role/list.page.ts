import { Component, computed, Injector, ViewChild } from "@angular/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { _HttpClient } from "@delon/theme";

import { AppPermission } from "@/gql";
import type { IRoleFilterInput } from "@/gql";
import { roleLists, setRoleDefault, deleteRole } from "../../graphql/role.operations.gql";
import type { RoleBrief } from "../../graphql/role.operations.gql";

import { RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import { ListPageLayoutComponent } from "@geexcode/geex-angular";
import { RoutedListComponent } from "@geexcode/geex-angular";

export type RoleParams = {
  pi: number;
  ps: number;
  name: string;
};

@Component({
  selector: "app-role-list",
  templateUrl: "./list.page.html",
  standalone: true,
  imports: [SharedModule, ListPageLayoutComponent],
})
export class RoleListComponent extends RoutedListComponent<RoleParams, RoleBrief> {
  override columns? = computed<Array<STColumn<RoleBrief>>>(() => [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    {
      title: this.I18N.Common.list.name,
      index: "name",
      className: "text-center",
    },
    {
      title: this.I18N.Identity.role.columnIsDefault,
      index: "isDefault",
      type: "yn",
      className: "text-center",
    },
    {
      title: this.I18N.Identity.user.columnCreatedOn,
      type: "date",
      index: "createdOn",
      className: "text-center",
    },
    {
      title: this.I18N.Common.list.actions,
      buttons: [
        {
          icon: "edit",
          text: this.I18N.Identity.role.setDefault,
          iif(item, btn, column) {
            return !item.isDefault;
          },
          iifBehavior: "disabled",
          click: item => this.setRoleDefault(item.id),
          acl: AppPermission.identity_mutation_editRole,
        },
        {
          icon: "edit",
          text: this.I18N.Common.action.edit,
          iifBehavior: "disabled",
          click: item => this.router.navigate(["edit", item.id], { relativeTo: this.route, queryParams: { roleName: item.name } }),
          acl: AppPermission.identity_mutation_editRole,
        },
        {
          icon: "delete",
          text: this.I18N.Common.action.delete,
          click: item => this.delete(item),
          acl: AppPermission.identity_mutation_editRole,
        },
      ],
      className: "text-center",
    },
  ]);

  override async onRouted(params: RoleParams) {
    let filter = undefined as IRoleFilterInput;
    if (params.name) {
      filter = {
        ...filter,
        name: {
          contains: params.name,
        },
      };
    }
    let res = await this.apollo
      .query<any>({
        query: roleLists,
        variables: {
          skip: (params.pi - 1) * params.ps,
          take: params.ps,
          filter,
        },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    this.total.set(res.data["roles"].totalCount);
    this.data.set(res.data["roles"].items);
  }
  override routeParamsMappings: RouteParamsMappings<RoleParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
    name: { position: "queryParams", default: "" },
  };

  selectChange(data: RoleBrief[]): void {
    //console.log(data);
  }

  batchApprove(approvePassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
  async setRoleDefault(id: string): Promise<any> {
    await this.apollo
      .mutate({
        mutation: setRoleDefault,
        variables: {
          roleId: id,
        },
        refetchQueries: [roleLists],
      })
      .firstValuePromise();
    this.refresh();
  }
  async sync() {
    this.msgSrv.warning(this.I18N.Identity.role.syncNotAvailable);
    return;
  }

  private async delete(item: RoleBrief) {
    this.nzModalSrv.confirm({
      nzTitle: this.I18N.Identity.role.confirmDelete.replace("{name}", item.name),
      nzOnOk: async () => {
        await this.apollo.mutate({ mutation: deleteRole, variables: { ids: [item.id] }, refetchQueries: [roleLists] }).firstValuePromise();
        this.refresh();
      },
    });
  }

  async batchDelete() {
    await this.batchOperation("delete", "Role");
  }
}


