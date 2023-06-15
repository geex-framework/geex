import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ApolloQueryResult } from "@apollo/client/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { SFComponent, SFSchema, SFValueChange } from "@delon/form";
import { ModalHelper, _HttpClient } from "@delon/theme";
import { ArrayService } from "@delon/util";
import { Apollo } from "apollo-angular";
import { NzContextMenuService, NzDropdownMenuComponent } from "ng-zorro-antd/dropdown";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/tree";

import {
  UserBriefFragment,
  AssignOrgsGql,
  UserListsQuery,
  UserListsQueryVariables,
  UserListFragment,
  UserListsGql,
} from "../../../../shared/graphql/.generated/type";

@Component({
  selector: "user-list",
  templateUrl: "./list.component.html",
})
export class OrgUserListComponent {
  @ViewChild("st")
  readonly st!: STComponent;
  pageNo = 0;
  pageSize = 10;
  activatedNode?: NzTreeNode;
  nodes: NzTreeNode[];
  columns: Array<STColumn<UserBriefFragment>> = [
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
  ];
  orgCode: string;
  users: UserListFragment[];
  selectedData: UserBriefFragment[] = [];
  constructor(private nzModalRef: NzModalRef, private apollo: Apollo, private msgSrv: NzMessageService) {
    // super(injector);
    this.prepare();
  }

  async prepare() {
    let res = await this.apollo
      .query<UserListsQuery, UserListsQueryVariables>({
        query: UserListsGql,
        variables: {},
      })
      .toPromise();
    this.st.clearStatus();
    this.selectedData = [];
    this.users = res.data.users.items;
    this.st.total = res.data.users.totalCount;
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
   * 带参数回传关闭
   *
   * @param result 回传参数
   */
  success(result: any = true): void {
    if (result) {
      this.nzModalRef.close(result);
    } else {
      this.close();
    }
  }

  close($event?: MouseEvent): void {
    this.nzModalRef.close();
  }
  async save() {
    if (!this.selectedData.any()) {
      this.msgSrv.warning("至少选择一项");
      return;
    }
    let maps = this.selectedData.map((x: UserListFragment) => {
      const userId = x.id;
      const orgs = x.orgCodes;
      const newOrgs = [...orgs, this.orgCode];
      return { userId, orgCodes: newOrgs };
    });
    // api
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
    this.success(this.selectedData.map(x => x.id));
  }
}
