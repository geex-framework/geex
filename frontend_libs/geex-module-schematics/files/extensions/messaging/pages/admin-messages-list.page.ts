import { Component, inject, OnInit, signal, TemplateRef, viewChild } from "@angular/core";
import { FormBuilder, Validators } from "@angular/forms";
import type { STChange, STColumn } from "@delon/abc/st";
import { geex, GEEX_I18N } from "@geexcode/geex-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";
import { SharedModule } from "@/shared/shared.module";
import type { MessagingBrief } from "../graphql/operations.gql";

@Component({
  selector: "app-messaging-admin-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./admin-messages-list.page.html",
})
export class MessagingAdminListPage implements OnInit {
  readonly I18N = inject(GEEX_I18N) as any;
  private readonly message = inject(NzMessageService);
  private readonly modal = inject(NzModalService);
  private readonly fb = inject(FormBuilder);
  private readonly createTpl = viewChild.required<TemplateRef<unknown>>("createTpl");
  private readonly sendTpl = viewChild.required<TemplateRef<unknown>>("sendTpl");
  readonly loading = signal(false);
  readonly data = signal<MessagingBrief[]>([]);
  readonly total = signal(0);
  pageIndex = 1;
  pageSize = 10;
  readonly severityOptions = ["INFO", "SUCCESS", "WARN", "ERROR", "FATAL"] as const;
  readonly createForm = this.fb.nonNullable.group({
    text: ["", Validators.required],
    severity: ["INFO" as (typeof this.severityOptions)[number], Validators.required],
  });
  readonly sendForm = this.fb.nonNullable.group({
    userIds: ["", Validators.required],
  });
  private sendTarget: MessagingBrief | null = null;

  readonly columns: Array<STColumn<MessagingBrief>> = [
    { title: this.I18N.Messaging.columnText, index: "title" },
    { title: this.I18N.Messaging.columnType, index: "messageType" },
    { title: this.I18N.Messaging.columnSeverity, index: "severity" },
    { title: this.I18N.Messaging.columnCreatedOn, index: "createdOn", type: "date" },
    {
      title: this.I18N.Messaging.columnActions,
      buttons: [
        { text: this.I18N.Messaging.send, click: item => this.openSend(item) },
        { text: this.I18N.Messaging.delete, type: "del", click: item => this.remove(item) },
      ],
    },
  ];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await geex.messaging.loadMessages({
        skip: (this.pageIndex - 1) * this.pageSize,
        take: this.pageSize,
      });
      this.data.set(result.items);
      this.total.set(result.totalCount);
    } finally {
      this.loading.set(false);
    }
  }

  onTableChange(change: STChange): void {
    if (change.type === "pi" || change.type === "ps") {
      this.pageIndex = change.pi ?? this.pageIndex;
      this.pageSize = change.ps ?? this.pageSize;
      void this.load();
    }
  }

  openCreate(): void {
    this.createForm.reset({ text: "", severity: "INFO" });
    this.modal.create({
      nzTitle: this.I18N.Messaging.createModalTitle,
      nzContent: this.createTpl(),
      nzOnOk: async () => {
        if (this.createForm.invalid) {
          this.createForm.markAllAsTouched();
          return false;
        }
        const { text, severity } = this.createForm.getRawValue();
        await geex.messaging.createMessage({ text: text.trim(), severity });
        this.message.success(this.I18N.Messaging.createSuccess);
        await this.load();
        return true;
      },
    });
  }

  openSend(item: MessagingBrief): void {
    this.sendTarget = item;
    this.sendForm.reset({ userIds: "" });
    this.modal.create({
      nzTitle: this.I18N.Messaging.sendModalTitle,
      nzContent: this.sendTpl(),
      nzOnOk: async () => {
        if (this.sendForm.invalid || !this.sendTarget) {
          this.sendForm.markAllAsTouched();
          return false;
        }
        const toUserIds = this.sendForm
          .getRawValue()
          .userIds.split(",")
          .map(x => x.trim())
          .filter(Boolean);
        if (!toUserIds.length) {
          return false;
        }
        await geex.messaging.sendMessage({ messageId: this.sendTarget.id, toUserIds });
        this.message.success(this.I18N.Messaging.sendSuccess);
        return true;
      },
    });
  }

  async remove(item: MessagingBrief): Promise<void> {
    await geex.messaging.deleteMessage(item.id);
    this.message.success(this.I18N.Messaging.deleteSuccess);
    await this.load();
  }
}
