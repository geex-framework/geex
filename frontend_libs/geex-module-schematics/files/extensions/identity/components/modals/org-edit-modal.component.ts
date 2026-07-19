import { Component, OnInit, Input, inject } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzNotificationService } from "ng-zorro-antd/notification";

import type { CreateOrgRequest, UpdateOrgRequest } from "@/gql";
import { createOrg as CreateOrgGql, updateOrg as UpdateOrgGql, orgs as OrgsGql, OrgBrief as OrgBriefFragment } from "@/modules/identity/graphql/org.operations.gql";
import { orgsCache as OrgsCacheGql } from "@/shared/graphql/queries.gql";

import { ModalComponentBase } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-edit",
  standalone: true,
  imports: [SharedModule],
  template: `
    <div class="modal-header">
      <div class="modal-title">{{ title }}</div>
    </div>
    <form nz-form #validFrom="ngForm" (ngSubmit)="submit()">
      <nz-form-item>
        <nz-form-label [nzSpan]="5" nzRequired nzFor="code">编码</nz-form-label>
        <nz-form-control [nzSpan]="12">
          <input name="code" type="text" [(ngModel)]="org.code" nz-input [disabled]="isEditMode" />
        </nz-form-control>
      </nz-form-item>
      <nz-form-item>
        <nz-form-label [nzSpan]="5" nzFor="name" nzRequired>名称</nz-form-label>
        <nz-form-control [nzSpan]="12">
          <input name="name" type="text" nz-input [(ngModel)]="org.name" />
        </nz-form-control>
      </nz-form-item>
      <nz-form-item>
        <nz-form-control [nzSpan]="12" [nzOffset]="5">
          <button nz-button type="submit" nzType="primary" [disabled]="validFrom.invalid">提交</button>
        </nz-form-control>
      </nz-form-item>
    </form>
  `,
  styles: [],
})
export class OrgEditModalComponent extends ModalComponentBase implements OnInit {
  @Input() org: Partial<CreateOrgRequest & { id?: string; parentCode?: string }>;

  get isEditMode(): boolean {
    return !!this.org?.id;
  }

  private apollo = inject(Apollo);
  private notify = inject(NzNotificationService);

  ngOnInit(): void {
    this.title = this.isEditMode ? "编辑组织" : "新增组织";
  }
  async submit() {
    try {
      if (this.isEditMode) {
        // 编辑组织
        await this.apollo
          .mutate({
            mutation: UpdateOrgGql,
            variables: {
              request: {
                id: this.org.id,
                name: this.org.name,
                // 编辑时不传递code，因为code不允许修改
              } as UpdateOrgRequest,
            },
            refetchQueries: [
              {
                query: OrgsGql,
              },
            ],
          })
          .firstValuePromise();
      } else {
        // 创建组织
        await this.apollo
          .mutate({
            mutation: CreateOrgGql,
            variables: {
              request: {
                name: this.org.name,
                code: this.org.code,
                parentCode: this.org.parentCode,
              } as CreateOrgRequest,
            },
            refetchQueries: [
              {
                query: OrgsGql,
              },
            ],
          })
          .firstValuePromise();
      }
      
      this.success(true);
      this.notify.success(`${this.title}成功`, "");
    } catch (error) {
      console.error(`${this.title}失败:`, error);
      this.notify.error(`${this.title}失败`, "请检查输入信息并重试");
    }
  }
}

