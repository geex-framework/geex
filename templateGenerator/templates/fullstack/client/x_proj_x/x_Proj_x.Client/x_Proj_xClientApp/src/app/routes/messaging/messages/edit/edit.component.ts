import { Location } from "@angular/common";
import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { SFComponent, SFSchema, SFUISchema } from "@delon/form";
import { _HttpClient } from "@delon/theme";
import { NzMessageService } from "ng-zorro-antd/message";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";

import { BusinessComponentBase } from "../../../../shared/components/business.component.base";
import {
  EditMessageGql,
  CreateMessageGql,
  Message,
  MessageSeverityType,
  MessagesGql,
  MessagesQuery,
  MessagesQueryVariables,
  MessageType,
} from "../../../../shared/graphql/.generated/type";
import { EditMode } from "../../../../shared/types/common";

@Component({
  selector: "app-messaging-edit",
  templateUrl: "./edit.component.html",
})
export class MessagingEditComponent extends BusinessComponentBase {
  mode: EditMode;
  id: string;
  data: Partial<Message>;
  @ViewChild("sf")
  readonly sf!: SFComponent;
  schema: SFSchema = {
    properties: {
      title: { type: "string", title: "标题", widget: "text" },
      messageType: {
        type: "string",
        title: "消息类型",
        widget: "select",
        enum: Object.entries(MessageType).map(x => ({ label: x[0], value: x[1] })),
      },
      severity: {
        type: "string",
        title: "重要性",
        widget: "select",
        enum: Object.entries(MessageSeverityType).map(x => ({ label: x[0], value: x[1] })),
      },
    },
    required: ["title", "messageType", "severity"],
  };
  ui: SFUISchema = {
    "*": {
      spanLabelFixed: 100,
      grid: { span: 12 },
    },
  };

  constructor(injector: Injector) {
    super(injector);
  }

  async prepare(params: any) {
    this.id = params.id;
    this.mode = params.id == undefined ? "create" : "edit";
    if (params.id) {
      let res = await this.apollo
        .query<MessagesQuery, MessagesQueryVariables>({
          query: MessagesGql,
          variables: {
            where: {
              id: {
                eq: params.id,
              },
            },
            includeDetail: true,
          },
        })
        .toPromise();
      this.data = res.data.messages.items[0];
    } else {
      this.data = {
        id: "",
        content: {
          _: "",
        },
        messageType: MessageType.Notification,
        severity: MessageSeverityType.Info,
        time: new Date(),
        createdOn: new Date(),
        title: "",
        toUserIds: [],
      };
    }
  }

  async submit(value: Partial<Message>): Promise<void> {
    if (this.mode == "create") {
      let res = await this.apollo
        .mutate({
          mutation: CreateMessageGql,
          variables: {
            input: {
              text: value.title,
              severity: value.severity,
            },
          },
        })
        .toPromise();
      if (res.data.createMessage.id) {
        this.msgSrv.success("创建成功");
      }
    } else {
      let res = await this.apollo
        .mutate({
          mutation: EditMessageGql,
          variables: {
            input: {
              id: this.id,
              text: value.title,
              messageType: value.messageType,
              severity: value.severity,
            },
          },
        })
        .toPromise();
      if (res.data.editMessage) {
        this.msgSrv.success("修改成功");
      }
    }
    this.router.navigate(["messaging", "view", this.id], { replaceUrl: true, forceReload: true });
  }
  processData(data: string) {
    return;
  }
}
