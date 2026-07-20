import { Component, OnInit, Input, inject } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzNotificationService } from "ng-zorro-antd/notification";
import { GEEX_I18N } from "@geexcode/geex-angular";

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
        <nz-form-label [nzSpan]="5" nzRequired nzFor="code">{{ I18N.Identity.org.codeLabel }}</nz-form-label>
        <nz-form-control [nzSpan]="12">
          <input name="code" type="text" [(ngModel)]="org.code" nz-input [disabled]="isEditMode" />
        </nz-form-control>
      </nz-form-item>
      <nz-form-item>
        <nz-form-label [nzSpan]="5" nzFor="name" nzRequired>{{ I18N.Common.list.name }}</nz-form-label>
        <nz-form-control [nzSpan]="12">
          <input name="name" type="text" nz-input [(ngModel)]="org.name" />
        </nz-form-control>
      </nz-form-item>
      <nz-form-item>
        <nz-form-control [nzSpan]="12" [nzOffset]="5">
          <button nz-button type="submit" nzType="primary" [disabled]="validFrom.invalid">{{ I18N.Identity.org.submit }}</button>
        </nz-form-control>
      </nz-form-item>
    </form>
  `,
  styles: [],
})
export class OrgEditModalComponent extends ModalComponentBase implements OnInit {
  @Input() org: Partial<CreateOrgRequest & { id?: string; parentCode?: string }>;

  readonly I18N = inject(GEEX_I18N) as any;

  get isEditMode(): boolean {
    return !!this.org?.id;
  }

  private apollo = inject(Apollo);
  private notify = inject(NzNotificationService);

  ngOnInit(): void {
    this.title = this.isEditMode ? this.I18N.Identity.org.editTitle : this.I18N.Identity.org.createTitle;
  }
  async submit() {
    try {
      if (this.isEditMode) {
        await this.apollo
          .mutate({
            mutation: UpdateOrgGql,
            variables: {
              request: {
                id: this.org.id,
                name: this.org.name,
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
      this.notify.success(this.isEditMode ? this.I18N.Identity.org.editSuccess : this.I18N.Identity.org.createSuccess, "");
    } catch (error) {
      console.error(error);
      this.notify.error(
        this.isEditMode ? this.I18N.Identity.org.editFailed : this.I18N.Identity.org.createFailed,
        this.I18N.Identity.org.operationFailedHint,
      );
    }
  }
}
