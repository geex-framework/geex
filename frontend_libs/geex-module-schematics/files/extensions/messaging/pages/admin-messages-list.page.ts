import { Component, inject, OnInit, signal } from "@angular/core";
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
  readonly loading = signal(false);
  readonly data = signal<MessagingBrief[]>([]);
  readonly total = signal(0);
  pageIndex = 1;
  pageSize = 10;
  readonly columns: Array<STColumn<MessagingBrief>> = [
    { title: this.I18N.Messaging.columnText, index: "text" },
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
    const form = this.fb.group({
      text: ["", Validators.required],
      severity: ["Info"],
    });
    this.modal.create({
      nzTitle: this.I18N.Messaging.createModalTitle,
      nzContent: `
        <form nz-form>
          <nz-form-item>
            <nz-form-label nzRequired>${this.I18N.Messaging.fieldText}</nz-form-label>
            <nz-form-control><textarea nz-input rows="3" id="msg-text"></textarea></nz-form-control>
          </nz-form-item>
        </form>
      `,
      nzOnOk: async () => {
        const text = (document.getElementById("msg-text") as HTMLTextAreaElement | null)?.value?.trim();
        if (!text) {
          return false;
        }
        await geex.messaging.createMessage({ text, severity: form.value.severity ?? "Info" });
        this.message.success(this.I18N.Messaging.createSuccess);
        await this.load();
        return true;
      },
    });
  }

  openSend(item: MessagingBrief): void {
    this.modal.create({
      nzTitle: this.I18N.Messaging.sendModalTitle,
      nzContent: `
        <form nz-form>
          <nz-form-item>
            <nz-form-label nzRequired>${this.I18N.Messaging.fieldUserIds}</nz-form-label>
            <nz-form-control><input nz-input id="msg-user-ids" placeholder="id1,id2" /></nz-form-control>
          </nz-form-item>
        </form>
      `,
      nzOnOk: async () => {
        const raw = (document.getElementById("msg-user-ids") as HTMLInputElement | null)?.value ?? "";
        const toUserIds = raw.split(",").map(x => x.trim()).filter(Boolean);
        if (!toUserIds.length) {
          return false;
        }
        await geex.messaging.sendMessage({ messageId: item.id, toUserIds });
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
