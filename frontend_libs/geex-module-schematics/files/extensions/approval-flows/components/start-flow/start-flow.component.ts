import { ChangeDetectorRef, Component, inject, Injectable, OnInit, ViewChild } from "@angular/core";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { NzEmptyModule } from "ng-zorro-antd/empty";
import { NzMessageService } from "ng-zorro-antd/message";
import { NZ_MODAL_DATA, NzModalRef, NzModalService } from "ng-zorro-antd/modal";
import { NzPaginationModule } from "ng-zorro-antd/pagination";
import { NzRadioModule } from "ng-zorro-antd/radio";
import { NzResultModule } from "ng-zorro-antd/result";
import { NzStepsModule } from "ng-zorro-antd/steps";

import {
  approvalFlowCategories,
  approvalFlowTemplates,
  approvalFlowTemplateById,
  createApprovalFlow,
  ApprovalFlowTemplateDetail,
} from "../../graphql/operations.gql";
import { ApprovalDynamicFormComponent } from "../dynamic-form/dynamic-form.component";
import { ApprovalUiValue, cleanValues } from "../dynamic-form/form-model";

import { ApprovalFlowAssociatedEntityType } from "@/gql";
import { SharedModule } from "@/shared/shared.module";

export interface StartApprovalFlowContext {
  title?: string;
  information?: string;
  entityType?: ApprovalFlowAssociatedEntityType;
  entityId?: string;
  entityName?: string;
  templateId?: string;
  onCreated?: (id: string) => void;
}

interface FlowDraft {
  name: string;
  description: string;
  values: ApprovalUiValue[];
  auditors: string[];
}

@Injectable({ providedIn: "root" })
export class StartApprovalFlowService {
  private readonly modal = inject(NzModalService);
  open(context: StartApprovalFlowContext = {}): NzModalRef {
    return this.modal.create({
      nzTitle: context.title ?? "发起审批",
      nzContent: StartApprovalFlowComponent,
      nzData: context,
      nzWidth: 920,
      nzFooter: null,
      nzMaskClosable: false,
    });
  }
}

@Component({
  selector: "approval-start-flow",
  standalone: true,
  imports: [SharedModule, NzStepsModule, NzResultModule, NzEmptyModule, NzRadioModule, NzPaginationModule, ApprovalDynamicFormComponent],
  template: `
    <nz-steps [nzCurrent]="step" nzSize="small" class="mb-lg">
      <nz-step nzTitle="选择审批流模板" /><nz-step nzTitle="填写流程表单" /><nz-step nzTitle="创建完成" />
    </nz-steps>
    @if (context.information) {
      <nz-alert nzType="info" [nzMessage]="context.information!" nzShowIcon class="mb-md" />
    }
    <div style="max-height:60vh;overflow:auto;padding:2px">
      @if (step === 0) {
        <div nz-row [nzGutter]="16" class="mb-md">
          <div nz-col [nzXs]="24" [nzMd]="12"
            ><nz-form-item
              ><nz-form-label>模板名称</nz-form-label
              ><nz-form-control>
                <input
                  nz-input
                  [(ngModel)]="search"
                  placeholder="按模板名称模糊搜索"
                  (keyup.enter)="loadTemplates(true)"
                /> </nz-form-control></nz-form-item
          ></div>
          <div nz-col [nzXs]="24" [nzMd]="12"
            ><nz-form-item
              ><nz-form-label>流程分类</nz-form-label
              ><nz-form-control>
                <nz-select
                  [(ngModel)]="category"
                  nzAllowClear
                  nzPlaceHolder="全部分类"
                  style="width:100%"
                  (ngModelChange)="loadTemplates(true)"
                >
                  @for (item of categories; track item) {
                    <nz-option [nzLabel]="item" [nzValue]="item" />
                  }
                </nz-select> </nz-form-control></nz-form-item
          ></div>
        </div>
        <button nz-button (click)="loadTemplates(true)" [nzLoading]="loading" class="mb-md">查询</button>
        <nz-radio-group [(ngModel)]="selectedId" style="width:100%">
          @for (template of templates; track template.id) {
            <nz-card class="mb-sm" nzSize="small" (click)="selectedId = template.id" style="cursor:pointer">
              <label nz-radio [nzValue]="template.id"
                ><strong>{{ template.name }}</strong></label
              >
              <p class="mt-sm mb-sm">{{ template.description }}</p>
              @for (tag of template.categories; track tag) {
                <nz-tag nzColor="purple">{{ tag }}</nz-tag>
              }
              <span>{{ template.formFields.length }} 个流程字段 / {{ template.nodes.length }} 个节点</span>
            </nz-card>
          } @empty {
            <nz-empty nzNotFoundContent="未找到符合条件的模板" />
          }
        </nz-radio-group>
        <nz-pagination [nzPageIndex]="page" [nzTotal]="total" [nzPageSize]="10" (nzPageIndexChange)="page = $event; loadTemplates()" />
      }
      @if (step === 1 && selected) {
        <h3>{{ selected.name }}</h3>
        <form nz-form [formGroup]="form" nzLayout="vertical">
          <nz-form-item
            ><nz-form-label nzRequired>流程名称</nz-form-label
            ><nz-form-control nzErrorTip="请填写流程名称"> <input nz-input formControlName="name" /> </nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>流程说明</nz-form-label
            ><nz-form-control>
              <textarea nz-input formControlName="description" rows="2"></textarea></nz-form-control
          ></nz-form-item>
        </form>
        <nz-divider nzText="流程表单" />
        <approval-dynamic-form [fields]="selected.formFields" [(values)]="draft.values" [editable]="true" [creating]="true" [flow]="true" />
        <nz-divider nzText="审批节点" />
        @for (node of selected.nodes; track node.id; let i = $index) {
          <nz-form-item
            ><nz-form-label nzRequired>{{ i + 1 }}. {{ node.name }}</nz-form-label
            ><nz-form-control>
              <nz-select [(ngModel)]="draft.auditors[i]" user-select style="width:100%" nzPlaceHolder="请选择节点审批人" />
              <small>{{ node.formFields.length }} 个节点字段, 由节点审批人维护</small>
            </nz-form-control></nz-form-item
          >
        }
      }
      @if (step === 2) {
        <nz-result nzStatus="success" nzTitle="审批流程已创建" [nzSubTitle]="createdName" />
      }
    </div>
    <div class="mt-lg" style="display:flex;justify-content:flex-end;gap:8px">
      @if (step < 2) {
        <button nz-button (click)="close()" [disabled]="saving">取消</button>
      }
      @if (step === 0) {
        <button nz-button nzType="primary" [disabled]="!selectedId || loading" (click)="next()">下一步, 填写表单</button>
      }
      @if (step === 1) {
        <button nz-button [disabled]="saving" (click)="back()">上一步</button>
        <button nz-button nzType="primary" [nzLoading]="saving" (click)="submit()">提交并创建流程</button>
      }
      @if (step === 2) {
        <button nz-button nzType="primary" (click)="close()">完成</button>
      }
    </div>
  `,
})
export class StartApprovalFlowComponent implements OnInit {
  readonly context = inject<StartApprovalFlowContext>(NZ_MODAL_DATA);
  private readonly apollo = inject(Apollo);
  private readonly modal = inject(NzModalRef);
  private readonly message = inject(NzMessageService);
  private readonly cdr = inject(ChangeDetectorRef);
  @ViewChild(ApprovalDynamicFormComponent) dynamicForm?: ApprovalDynamicFormComponent;
  readonly form = new FormGroup({
    name: new FormControl("", { nonNullable: true, validators: Validators.required }),
    description: new FormControl("", { nonNullable: true }),
  });
  step = 0;
  loading = false;
  saving = false;
  search = "";
  category?: string;
  categories: string[] = [];
  templates: ApprovalFlowTemplateDetail[] = [];
  selectedId?: string;
  selected?: ApprovalFlowTemplateDetail;
  page = 1;
  total = 0;
  createdName = "";
  createdId?: string;
  draft: FlowDraft = { name: "", description: "", values: [], auditors: [] };
  private readonly drafts = new Map<string, FlowDraft>();
  private loadSequence = 0;

  async ngOnInit(): Promise<void> {
    this.selectedId = this.context.templateId;
    const result = await this.apollo.query({ query: approvalFlowCategories, fetchPolicy: "no-cache" }).firstValuePromise();
    this.categories = result.data.approvalFlowCategories;
    this.cdr.markForCheck();
    await this.loadTemplates();
  }

  async loadTemplates(reset = false): Promise<void> {
    if (reset) this.page = 1;
    const sequence = ++this.loadSequence;
    this.loading = true;
    try {
      const result = await this.apollo
        .query({
          query: approvalFlowTemplates,
          variables: { request: { name: this.search || null, category: this.category || null }, skip: (this.page - 1) * 10, take: 10 },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      if (sequence !== this.loadSequence) return;
      this.templates = (result.data.approvalFlowTemplate?.items ?? []).filter((x): x is ApprovalFlowTemplateDetail => !!x);
      this.total = result.data.approvalFlowTemplate?.totalCount ?? 0;
    } finally {
      if (sequence === this.loadSequence) this.loading = false;
      this.cdr.markForCheck();
    }
  }

  async next(): Promise<void> {
    if (!this.selectedId) return;
    const result = await this.apollo
      .query({ query: approvalFlowTemplateById, variables: { id: this.selectedId }, fetchPolicy: "no-cache" })
      .firstValuePromise();
    const template = result.data.approvalFlowTemplateById;
    if (!template) {
      this.message.error("审批流模板不存在");
      return;
    }
    this.selected = template;
    this.draft = this.drafts.get(template.id) ?? {
      name: this.context.entityName ? `${template.name} - ${this.context.entityName}` : template.name,
      description: template.description ?? "",
      values: [],
      auditors: template.nodes.map(node => node.auditUserId ?? ""),
    };
    this.form.setValue({ name: this.draft.name, description: this.draft.description });
    this.step = 1;
    this.cdr.markForCheck();
  }

  back(): void {
    if (!this.selected) return;
    if (!this.dynamicForm?.filesReady()) return;
    this.drafts.set(this.selected.id, {
      ...this.draft,
      ...this.form.getRawValue(),
      values: this.dynamicForm?.readValues() ?? this.draft.values,
    });
    this.step = 0;
  }

  async submit(): Promise<void> {
    if (this.saving || !this.selected) return;
    if (!this.dynamicForm?.filesReady()) return;
    this.form.markAllAsTouched();
    if (!this.form.valid || !this.dynamicForm?.validate() || this.draft.auditors.some(id => !id)) {
      this.message.warning("请完善流程表单及各节点审批人");
      return;
    }
    this.saving = true;
    try {
      const result = await this.apollo
        .mutate({
          mutation: createApprovalFlow,
          variables: {
            request: {
              ...this.form.getRawValue(),
              templateId: this.selected.id,
              orgCode: this.selected.orgCode,
              associatedEntityType: this.context.entityType,
              associatedEntityId: this.context.entityId,
              formValues: cleanValues(this.dynamicForm.readValues()),
              nodes: this.selected.nodes.map((node, index) => ({
                name: node.name,
                index,
                auditUserId: this.draft.auditors[index],
                auditRole: node.auditRole,
                carbonCopyUserIds: node.carbonCopyUserIds ?? [],
              })),
            },
          },
        })
        .firstValuePromise();
      const flow = result.data?.createApprovalFlow;
      if (flow) {
        this.createdId = flow.id;
        this.createdName = flow.name;
        this.step = 2;
        this.context.onCreated?.(flow.id);
      }
    } finally {
      this.saving = false;
      this.cdr.markForCheck();
    }
  }

  close(): void {
    if (!this.saving) this.modal.close(this.createdId);
  }
}
