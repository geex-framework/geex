import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ApolloQueryResult } from "@apollo/client/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { SFComponent, SFSchema, SFValueChange } from "@delon/form";
import { ModalHelper, _HttpClient } from "@delon/theme";
import { ArrayService } from "@delon/util";
import { NzContextMenuService, NzDropdownMenuComponent } from "ng-zorro-antd/dropdown";
import { NzModalRef } from "ng-zorro-antd/modal";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/tree";

import { BusinessComponentBase } from "../../../shared/components";
import {
  OrgsQuery,
  OrgsQueryVariables,
  OrgsGql,
  AssignOrgsGql,
  AppPermission,
  UserListFragment,
  UserListsQuery,
  UserListsGql,
  UserListsQueryVariables,
  OrgBriefGql,
  OrgBriefFragment,
  OrgCacheItem,
  OrgTypeEnum,
} from "../../../shared/graphql/.generated/type";
import { OrgEditComponent } from "./edit/edit.component";
import { OrgUserListComponent } from "./user-list/list.component";
export interface orgInfo {
  code: string;
  name: string;
  // orgType: OrgTypeEnum;
  // parentORgCode: string;
}

@Component({
  selector: "app-org-list",
  templateUrl: "./list.component.html",
  styles: [
    `
      .danger-text {
        color: red;
      }
    `,
  ],
})
export class OrgListComponent extends BusinessComponentBase implements OnInit {
  /**右键菜单 */
  contextMenus: NzDropdownMenuComponent;
  @ViewChild("st")
  readonly st!: STComponent;
  pageNo = 0;
  pageSize = 10;
  activatedNode?: NzTreeNode;
  nodes: NzTreeNode[];
  orgBrief: OrgBriefFragment[] = [];
  modalHelper: ModalHelper;
  AppPermission = AppPermission;
  columns: Array<STColumn<UserListFragment>> = [
    {
      width: 35,
      type: "checkbox",
      index: "checked",
      className: "text-center",
    },
    {
      title: "用户名",
      index: "username",
      className: "text-center",
    },
    {
      title: "邮箱",
      index: "email",
      className: "text-center",
    },
    {
      title: "创建时间",
      index: "createdOn",
      type: "date",
      className: "text-center",
    },
    {
      title: "操作",
      buttons: [
        {
          icon: "delete",
          text: "移除",
          type: "del",
          className: "text-center",
          click: item => this.delete(item.id),
        },
      ],
    },
  ];
  users: UserListFragment[];
  selectedData: UserListFragment[] = [];
  constructor(
    injector: Injector,
    private nzContextMenuService: NzContextMenuService,
    private arrService: ArrayService,
    private modalSrv: ModalHelper,
  ) {
    super(injector);
  }

  async prepare(params: any) {
    let res = await this.apollo
      .query<OrgsQuery, OrgsQueryVariables>({
        query: OrgsGql,
      })
      .toPromise();

    let data = res.data.orgs.items.map(x => ({
      code: x.code,
      name: x.name,
      id: x.id,
      expanded: x.code.lastIndexOf(".") == -1,
      pCode: x.code.substring(0, x.code.lastIndexOf(".")),
    }));
    this.nodes = this.arrService.arrToTreeNode(data, {
      parentIdMapName: "pCode",
      idMapName: "code",
      titleMapName: "name",
    });
    this.activatedNode = undefined;
    this.orgBrief = res.data.orgs.items;
    console.log(this.orgBrief);
  }

  openFolder(data: NzTreeNode | NzFormatEmitEvent): void {
    // do something if u want
    if (data instanceof NzTreeNode) {
      data.isExpanded = !data.isExpanded;
    } else {
      const node = data.node;
      if (node) {
        node.isExpanded = !node.isExpanded;
      }
    }
  }

  activeNode(data: NzFormatEmitEvent): void {
    if (data.node.isSelected) {
      this.pageNo = 0;
      this.pageSize = 10;
      this.activatedNode = data.node!;
      this.getOrgUsers();
    } else {
      this.activatedNode = undefined;
    }
  }
  async getOrgUsers() {
    let res = await this.apollo
      .query<UserListsQuery, UserListsQueryVariables>({
        query: UserListsGql,
        variables: {
          skip: this.pageNo,
          take: this.pageSize,
          where: {
            orgCodes: {
              some: {
                eq: this.activatedNode.key,
              },
            },
          },
        },
      })
      .toPromise();
    this.st.clearStatus();
    this.selectedData = [];
    this.st.total = res.data.users.totalCount;
    this.users = res.data.users.items;
  }
  contextMenu($event: MouseEvent, menu: NzDropdownMenuComponent): void {
    this.nzContextMenuService.create($event, menu);
  }
  createOrg(parentCode?: string) {
    const org = {
      name: undefined,
      code: undefined,
      parentCode,
    };
    // todo Api需要parentId,这里不直接拿取this.activenode.id是避免单机节点激活后点击头部的新增按钮
    // const parentId = this.orgBrief.find(x => x.code == parentCode).id;
    this.modalSrv.createStatic(OrgEditComponent, { org }, { size: 500, exact: true }).subscribe(res => {
      if (res) {
        this.refresh();
      }
    });
  }
  addUser() {
    this.modalSrv.createStatic(OrgUserListComponent, { orgCode: this.activatedNode.key }).subscribe(res => {
      if (res) {
        this.getOrgUsers();
      }
    });
  }
  selectDropdown(): void {
    // do something
  }
  change(args: STChange): void {
    if (args.type === "pi" || args.type === "ps") {
      this.pageSize = args.pi;
      this.pageNo = args.ps;
    }
    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
    }
  }

  /**
   * 移除单个用户
   *
   * @param {string} id
   * @memberof OrgListComponent
   */
  async delete(id: string) {
    const orgCodes = this.users.first(x => x.id == id).orgCodes;
    let newOrgCodes = orgCodes.removeAll(x => x == this.activatedNode.key);
    // api
    await this.apollo
      .mutate({
        mutation: AssignOrgsGql,
        variables: {
          input: {
            userOrgsMap: [{ userId: id, orgCodes: newOrgCodes }],
          },
        },
      })
      .toPromise();
    this.getOrgUsers();
  }

  async batchDelete() {
    if (!this.selectedData.any()) {
      this.msgSrv.warning("至少选择一项");
      return;
    }
    this.nzModalSrv.confirm({
      nzTitle: "确定批量移除用户？",
      nzOkText: "确定",
      nzCancelText: "取消",
      nzOnOk: async () => {
        let maps = this.selectedData.map((x: UserListFragment) => {
          const userId = x.id;
          const orgs = x.orgCodes;
          const newOrgs = orgs.removeAll(x => x == this.activatedNode.key);
          return { userId, orgCodes: newOrgs };
        });
        await this.apollo
          .mutate({
            mutation: AssignOrgsGql,
            variables: {
              input: {
                userOrgsMap: maps,
              },
            },
          })
          .toPromise();
        this.getOrgUsers();
      },
    });
  }
  refresh() {
    this.activatedNode = undefined;
    this.router.navigate([], { forceReload: true });
  }
  createContextMenu($event: MouseEvent, menu: NzDropdownMenuComponent, node: NzTreeNode): void {
    this.nzContextMenuService.create($event, menu);
    this.contextMenus = menu;
    this.activatedNode = node;
  }

  editUnit(): void {
    console.log(this.activatedNode);
    const item = this.orgBrief.find(x => x.code === this.activatedNode?.key);
    if (this.activatedNode?.key) {
      console.log(item);
      this.modalSrv.createStatic(OrgEditComponent, { org: item }, { size: 500 }).subscribe(res => {
        if (res) {
          this.refresh();
        }
      });
    }
    this.nzContextMenuService.close();
  }

  addSubUnit(): void {
    if (this.activatedNode.key) {
      this.createOrg(this.activatedNode.key);
    }
    this.nzModalSrv.ngOnDestroy();
    this.nzContextMenuService.close();
  }

  deleteUnit(): void {
    const itemId = this.activatedNode.origin.id;
    console.log(itemId);
    // todo 删除接口
    this.nzContextMenuService.close();
  }
}
