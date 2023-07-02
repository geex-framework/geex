import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { ModalHelper, _HttpClient } from "@delon/theme";

import {
  AppPermission,
  RoleBriefFragment,
  RoleFilterInput,
  RoleListsGql,
  RoleListsQuery,
  RoleListsQueryVariables,
  SetRoleDefaultGql,
} from "../../../shared/graphql/.generated/type";

import { RoutedComponent } from "@/app/shared/components/routed-components/routed.component.base";

export type RoleParams = {
  pi: number;
  ps: number;
  name: string;
};

type RoleListComponentData = {
  data: RoleBriefFragment[];
  total: number;
};

@Component({
  selector: "app-role-list",
  templateUrl: "./list.component.html",
})
export class RoleListComponent extends RoutedComponent<RoleParams, RoleListComponentData> {
  data: RoleBriefFragment[];
  AppPermission = AppPermission;
  selectedData: RoleBriefFragment[];
  name: string;
  @ViewChild("st")
  readonly st!: STComponent;
  columns: Array<STColumn<RoleBriefFragment>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    // { title: "Id", index: "id" },
    {
      title: "名称",
      index: "name",
      className: "text-center",
    },
    {
      title: "默认角色",
      index: "isDefault",
      type: "yn",
      className: "text-center",
    },
    {
      title: "创建时间",
      type: "date",
      index: "createdOn",
      className: "text-center",
    },
    {
      title: "操作",
      buttons: [
        {
          icon: "edit",
          text: "设为默认角色",
          iif(item, btn, column) {
            return !item.isDefault;
          },
          iifBehavior: "disabled",
          click: item => this.setRoleDefault(item.id),
          acl: AppPermission.IdentityMutationEditRole,
        },
        {
          icon: "edit",
          text: "编辑",
          // iif(item, btn, column) {
          //   let enabled = !item.isStatic;
          //   if (!enabled) {
          //     btn.tooltip = "系统角色不可编辑.";
          //   }
          //   return enabled;
          // },
          iifBehavior: "disabled",
          click: item => this.router.navigate(["edit", item.id], { relativeTo: this.route, queryParams: { roleName: item.name } }),
          acl: AppPermission.IdentityMutationEditRole,
        },
      ],
      className: "text-center",
    },
  ];

  constructor(injector: Injector) {
    super(injector);
  }

  async fetchData(): Promise<RoleListComponentData> {
    let params = this.params.value;
    let where = undefined as RoleFilterInput;
    if (params.name) {
      where = {
        ...where,
        name: {
          eq: params.name,
        },
      };
    }
    let res = await this.apollo
      .query<RoleListsQuery, RoleListsQueryVariables>({
        query: RoleListsGql,
        variables: {
          skip: Number(((params.pi ?? 1) - 1) * (this.st?.ps ?? 10)),
          take: Number(params.ps ?? this.st?.ps ?? 10),
          where,
        },
      })
      .toPromise();
    this.st.clearStatus();
    this.st.total = res.data.roles.totalCount;
    this.loading = res.loading;
    this.data = res.data.roles.items;
    this.selectedData = [];
    return {
      total: res.data.roles.totalCount,
      data: res.data.roles.items,
    };
  }

  stChange(args: STChange) {
    if (args.type == "pi" || args.type == "ps") {
      this.router.navigate([], { queryParams: { pi: args.pi, ps: args.ps, name: this.params.value.name } });
    }

    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
    }
  }

  selectChange(data: RoleBriefFragment[]): void {
    //console.log(data);
  }

  batchAudit(auditPassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
  async setRoleDefault(id: string): Promise<any> {
    await this.apollo
      .mutate({
        mutation: SetRoleDefaultGql,
        variables: {
          roleId: id,
        },
      })
      .toPromise();
    this.refresh();
  }
  async sync() {
    this.msgSrv.warning("同步功能未开放");
    return;
  }
}
