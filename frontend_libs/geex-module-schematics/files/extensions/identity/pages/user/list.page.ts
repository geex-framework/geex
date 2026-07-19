import { Component, computed, inject, signal } from "@angular/core";
import { STColumn } from "@delon/abc/st";
import { ModalHelper, _HttpClient } from "@delon/theme";

import { RoutedListComponent } from "@geexcode/geex-angular";
import { RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import { ListPageLayoutComponent } from "@geexcode/geex-angular";
import { AppPermission, SortEnumType } from "@/gql";
import type { IUserFilterInput } from "@/gql";
import { userLists, deleteUser } from "../../graphql/user.operations.gql";
import { UserList } from "../../graphql/user.operations.gql";
export type UserListParams = {
  pi: number;
  ps: number;
  username: string;
  sort: { [key in keyof UserList]?: SortEnumType };
  org: string[];
  createDate: Date[];
};
export interface orgInfo {
  code: string;
  name: string;
}

@Component({
  selector: "app-user-list",
  templateUrl: "./list.page.html",
  standalone: true,
  imports: [SharedModule, ListPageLayoutComponent],
  styles: [
    `
      .danger-text {
        color: red;
      }
    `,
  ],
})
export class UserListPage extends RoutedListComponent<UserListParams, UserList> {
  override routeParamsMappings: RouteParamsMappings<UserListParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
    username: { position: "queryParams", default: "" },
    sort: { position: "queryParams", default: null },
    org: { position: "queryParams", default: [] },
    createDate: {
      position: "queryParams",
      default: [],
    },
  };
  override async onRouted(params: UserListParams) {
    let filter = undefined as IUserFilterInput;
    if (params.username) {
      filter = {
        ...filter,
        username: {
          contains: params.username,
        },
      };
    }
    const selectedOrgs = params.org;
    if (selectedOrgs && selectedOrgs.length > 0) {
      filter = {
        ...filter,
        orgCodes: {
          some: {
            in: selectedOrgs,
          },
        },
      };
    }
    const [start, end] = params.createDate;
    if (start) {
      filter = {
        ...filter,
        createdOn: {
          gte: start,
        },
      };
    }

    if (end) {
      filter = {
        ...filter,
        createdOn: {
          ...filter.createdOn,
          lte: end,
        },
      };
    }

    let res: any = await this.apollo
      .query({
        query: userLists,
        variables: {
          skip: Number(((params.pi ?? 1) - 1) * (params?.ps ?? 10)),
          take: Number(params.ps ?? params?.ps ?? 10),
          filter,
          sort: params.sort,
        },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    this.selectedData.set([]);
    this.total.set(res.data.users.totalCount);
    this.data.set(res.data.users.items);
  }
  override columns? = computed<STColumn<UserList>[]>(() => [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    {
      title: "Id",
      index: "id",
      className: ["text-center"],
      sort: { key: "id", default: this.params().sort?.id },
    },
    {
      title: "用户名",
      index: "username",
      className: ["text-center"],
    },
    {
      title: "昵称",
      index: "nickname",
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
      sort: { key: "createdOn", default: this.params().sort?.createdOn },
      type: "date",
    },
    {
      title: "操作",
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: item => this.router.navigate(["edit", item.id], { relativeTo: this.route }),
          acl: AppPermission.identity_mutation_editUser,
        },
        {
          icon: "delete",
          text: "删除",
          click: item => this.delete(item),
          acl: AppPermission.identity_mutation_editUser,
        },
      ],
      className: ["text-center"],
    },
  ]);
  override selectedData = signal<UserList[]>([]);

  createUser() {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }

  private async delete(item: UserList) {
    this.nzModalSrv.confirm({
      nzTitle: `确认删除用户"${item.username}"?`,
      nzOnOk: async () => {
        await this.apollo.mutate({ mutation: deleteUser, variables: { ids: [item.id] }, refetchQueries: [userLists] }).firstValuePromise();
        this.refresh();
      },
    });
  }
}


