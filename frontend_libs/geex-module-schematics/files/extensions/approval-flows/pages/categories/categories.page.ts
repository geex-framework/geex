import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";

import { approvalFlowCategories, saveApprovalFlowCategories } from "../../graphql/operations.gql";

import { AppPermission } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "approval-categories",
  standalone: true,
  imports: [SharedModule],
  template: `
    <page-header title="流程分类配置" />
    <nz-card [nzLoading]="loading">
      <form nz-form nzLayout="vertical">
        <nz-form-item>
          <nz-form-label>流程分类</nz-form-label>
          <nz-form-control nzExtra="输入分类名称后按回车添加, 点击标签上的叉号移除, 完成后统一保存.">
            <nz-select
              name="categories"
              nzMode="tags"
              [(ngModel)]="categories"
              [nzDisabled]="saving"
              nzPlaceHolder="输入分类名称后按回车添加"
              style="width:100%"
            >
              @for (category of categories; track category) {
                <nz-option [nzValue]="category" [nzLabel]="category" [nzHide]="true" />
              }
            </nz-select>
          </nz-form-control>
        </nz-form-item>
        <p>流程模板和审批事项可关联这些分类, 并按分类筛选.</p>
        <button nz-button nzType="primary" type="button" [acl]="permission" [nzLoading]="saving" (click)="save()">保存</button>
      </form>
    </nz-card>
  `,
})
export class ApprovalCategoriesPage implements OnInit {
  private readonly apollo = inject(Apollo);
  private readonly message = inject(NzMessageService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly permission = AppPermission.settings_mutation_editSetting;
  loading = false;
  saving = false;
  categories: string[] = [];
  async ngOnInit(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.apollo.query({ query: approvalFlowCategories, fetchPolicy: "no-cache" }).firstValuePromise();
      this.categories = result.data.approvalFlowCategories;
    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }
  async save(): Promise<void> {
    if (this.saving) return;
    const categories = this.categories.map(x => x.trim()).filter(Boolean);
    if (new Set(categories).size !== categories.length) {
      this.message.warning("流程分类不能重复");
      return;
    }
    this.saving = true;
    try {
      await this.apollo.mutate({ mutation: saveApprovalFlowCategories, variables: { categories } }).firstValuePromise();
      this.categories = categories;
      this.message.success("流程分类已保存");
    } finally {
      this.saving = false;
      this.cdr.markForCheck();
    }
  }
}
