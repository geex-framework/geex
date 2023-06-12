import { Component, Injector, Input, OnInit, ViewChild } from "@angular/core";
import { AbstractControl, FormBuilder, FormGroup, Validators } from "@angular/forms";
import { SFSchema, SFUISchema } from "@delon/form";
import { SettingsService, User } from "@delon/theme";
import { ArrayService, deepCopy } from "@delon/util";
import { Select, Store } from "@ngxs/store";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/tree";
import { Observable } from "rxjs";
import { map, take } from "rxjs/operators";

import { AuditBadge, BusinessComponentBase } from "../../shared/components";
import {
  ChangePasswordGql,
  ChangePasswordRequestInput,
  Org,
  OrgBriefFragment,
  OrgCacheItemFragment,
  UserDetailFragment,
} from "../../shared/graphql/.generated/type";
import { CacheDataState } from "../../shared/states/cache-data.state";
import { UserDataState } from "../../shared/states/user-data.state";

@Component({
  selector: "app-personal-center-list",
  templateUrl: "./list.component.html",
})
export class PersonalCenterListComponent extends BusinessComponentBase {
  @Select(UserDataState)
  userData$: Observable<UserDetailFragment>;
  orgs$: Observable<OrgCacheItemFragment[]>;
  validateForm!: FormGroup;
  id: string;
  nodes: NzTreeNode[];
  pageNo = 0;
  pageSize = 10;
  activatedNode?: NzTreeNode;
  isVisible = false;
  confirmPassword: string;
  data: ChangePasswordRequestInput = {
    originPassword: undefined,
    newPassword: undefined,
  };
  get user(): User {
    return this.settings.user;
  }
  constructor(injector: Injector, private settings: SettingsService, private arrService: ArrayService, private fb: FormBuilder) {
    super(injector);
    this.orgs$ = injector
      .get(Store)
      .select(CacheDataState)
      .pipe(map(x => x.orgs));
  }
  async prepare(params: any) {
    let userData = await this.userData$.pipe(take(1)).toPromise();
    let orgs = userData.orgs.concat(userData.orgs.selectMany(x => x.allParentOrgs as any)).distinctBy(x => x.code);
    let data = orgs.map(x => ({
      code: x.code,
      name: x.name,
      expanded: x.code.lastIndexOf(".") == -1,
      pCode: x.code.substring(0, x.code.lastIndexOf(".")),
    }));

    // 此处可能有多个根节点, 不能使用arrToTreeNode
    this.nodes = this.arrService.arrToTreeNode(data, {
      parentIdMapName: "pCode",
      idMapName: "code",
      titleMapName: "name",
    });
    this.activatedNode = undefined;
  }

  activeNode(data: NzFormatEmitEvent): void {
    if (data.node.isSelected) {
      this.pageNo = 0;
      this.pageSize = 10;
      this.activatedNode = data.node!;
    } else {
      this.activatedNode = undefined;
    }
  }

  refresh() {
    this.router.navigate([]);
  }

  showModal() {
    this.isVisible = true;
  }

  async handleOk() {
    await this.apollo
      .mutate({
        mutation: ChangePasswordGql,
        variables: {
          input: {
            originPassword: this.data.originPassword,
            newPassword: this.data.newPassword,
          },
        },
      })
      .toPromise();
    this.msgSrv.success("修改成功");
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;

    this.isVisible = false;
  }

  handleCancel(): void {
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;
    this.isVisible = false;
  }
}
