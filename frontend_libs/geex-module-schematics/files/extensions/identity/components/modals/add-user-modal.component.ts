import { Component, inject, Injector } from "@angular/core";
import { STChange, STColumn } from "@delon/abc/st";
import { _HttpClient } from "@delon/theme";
import { NzModalRef } from "ng-zorro-antd/modal";
import { NzTreeNode } from "ng-zorro-antd/tree";

import type {
  UserBrief as UserBriefFragment,
  UserList as UserListFragment,
  userListsResult as UserListsQuery,
  userListsVariables as UserListsQueryVariables,
} from "@/modules/identity/graphql/user.operations.gql";
import { userLists as UserListsGql, assignOrgs as AssignOrgsGql } from "@/modules/identity/graphql/user.operations.gql";
import { BusinessComponentBase } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

@Component({
  standalone: true,
  imports: [SharedModule],
  template: `
    <div class="modal-header">
      <div class="modal-title">{{ I18N.Identity.modal.selectUser }}</div>
    </div>
    <nz-alert style="margin: 10px 0;" nzType="info" [nzMessage]="message" nzShowIcon>
      <ng-template #message>
        <span>{{ I18N.Common.list.selected }}{{ selectedData.length }}{{ I18N.Common.list.selectedUnit }}</span>
        <a (click)="st.clearStatus(); st.reset($event)"> {{ I18N.Common.list.clearSelection }}</a>
      </ng-template>
    </nz-alert>
    <st #st [data]="users" [columns]="columns" (change)="change($event)"></st>
    <div class="modal-footer">
      <button nz-button [nzType]="'default'" (click)="close()"> {{ I18N.Common.action.cancel }} </button>
      <button nz-button [nzType]="'primary'" (click)="save()"> {{ I18N.Common.action.confirm }} </button>
    </div>
  `,
})
export class AddUserModalComponent extends BusinessComponentBase {
  pageNo = 0;
  pageSize = 10;
  activatedNode?: NzTreeNode;
  nodes: NzTreeNode[];
  columns: Array<STColumn<UserBriefFragment>>;
  orgCode: string;
  users: UserListFragment[];
  total = 0;
  selectedData: UserBriefFragment[] = [];
  private nzModalRef = inject(NzModalRef);
  constructor() {
    super();
    this.columns = [
      {
        width: 35,
        type: "checkbox",
        index: "checked",
        className: "text-center",
      },
      {
        title: this.I18N.Identity.user.columnUsername,
        index: "username",
        className: "text-center",
      },
      {
        title: this.I18N.Identity.user.columnEmail,
        index: "email",
        className: "text-center",
      },
      {
        title: this.I18N.Identity.user.columnCreatedOn,
        index: "createdOn",
        type: "date",
        className: "text-center",
      },
    ];
    this.prepare();
  }

  async prepare() {
    let res = await this.apollo
      .query<UserListsQuery, UserListsQueryVariables>({
        query: UserListsGql,
        variables: {},
      })
      .firstValuePromise();
    const notInOrg = res.data.users.items.where(x => ![...x.orgCodes].contains(this.orgCode));
    this.selectedData = [];
    this.users = notInOrg;
    this.total = res.data.users.totalCount;
  }
  selectDropdown(): void {
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
      this.msgSrv.warning(this.I18N.Identity.modal.selectAtLeastOne);
      return;
    }
    let maps = this.selectedData.map((x: UserListFragment) => {
      const userId = x.id;
      const orgs = x.orgCodes;
      const newOrgs = [...orgs, this.orgCode];
      return { userId, orgCodes: newOrgs };
    });
    await this.apollo
      .mutate({
        mutation: AssignOrgsGql,
        variables: {
          request: {
            userOrgsMap: maps,
          },
        },
      })
      .firstValuePromise();
    this.success(this.selectedData.map(x => x.id));
  }
}
