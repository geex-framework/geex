import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { ModalHelper, _HttpClient } from "@delon/theme";

import {
  AppPermission,
  IUserFilterInput,
  UserBriefFragment,
  UserListsGql,
  UserListsQuery,
  UserListsQueryVariables,
} from "../../../shared/graphql/.generated/type";

import { RoutedComponent } from "@/app/shared/components/routed-components/routed.component.base";
export type UserParams = {
  pi: number;
  ps: number;
  username: string;
};

type RoleListComponentData = {
  data: UserBriefFragment[];
  total: number;
};
@Component({
  selector: "app-user-list",
  templateUrl: "./list.component.html",
})
export class UserListComponent extends RoutedComponent<UserParams, RoleListComponentData> {
  data: UserBriefFragment[];
  selectedData: UserBriefFragment[];
  AppPermission = AppPermission;
  @ViewChild("st")
  readonly st!: STComponent;
  columns: Array<STColumn<UserBriefFragment>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    { title: "Id", index: "id", className: ["text-center"] },
    {
      title: "用户名",
      index: "username",
      // render: "userName",
      className: ["text-center"],
    },
    {
      title: "昵称",
      index: "nickname",
      // render: "userName",
      className: ["text-center"],
    },
    {
      title: "邮箱",
      index: "email",
      className: ["text-center"],
    },
    {
      title: "手机号",
      index: "phoneNumber",
      className: ["text-center"],
    },
    {
      title: "角色",
      index: "roleNames",
      className: ["text-center"],
    },
    {
      width: 60,
      title: "是否激活",
      index: "isEnable",
      type: "widget",
      widget: { type: "yn-export", params: ({ record }) => ({ value: record.isEnable }) },
      format: item => (item.isEnable ? "是" : "否"),
      className: "text-center",
    },
    {
      title: "创建时间",
      index: "createdOn",
      type: "date",
    },
    {
      title: "操作",
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: item => this.router.navigate(["edit", item.id], { relativeTo: this.route }),
          acl: AppPermission.IdentityMutationEditUser,
        },
      ],
      className: ["text-center"],
    },
  ];

  constructor(injector: Injector) {
    super(injector);
  }

  async fetchData(): Promise<RoleListComponentData> {
    let params = this.params.value;
    let where = undefined as IUserFilterInput;
    if (params.username) {
      where = {
        ...where,
        username: {
          eq: params.username,
        },
      };
    }
    let res = await this.apollo
      .query<UserListsQuery, UserListsQueryVariables>({
        query: UserListsGql,
        variables: {
          skip: Number(((params.pi ?? 1) - 1) * (this.st?.ps ?? 10)),
          take: Number(params.ps ?? this.st?.ps ?? 10),
          where,
        },
      })
      .toPromise();
    this.st.clearStatus();
    this.st.total = res.data.users.totalCount;
    this.loading = res.loading;
    this.data = res.data.users.items;
    this.selectedData = [];
    return {
      total: res.data.users.totalCount,
      data: res.data.users.items,
    };
  }

  stChange(args: STChange) {
    if (args.type == "pi" || args.type == "ps") {
      this.router.navigate([], { queryParams: { pi: args.pi, ps: args.ps, username: this.params.value.username } });
    }

    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
    }
  }

  selectChange(data: UserBriefFragment[]): void {}

  batchAudit(auditPassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
  //以,分隔的用户工号列表
  openIds: string = "";
}
