import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ApolloQueryResult } from "@apollo/client/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { SFComponent, SFSchema, SFValueChange } from "@delon/form";
import { ModalHelper, _HttpClient } from "@delon/theme";
import { Apollo } from "apollo-angular";
import html from "html-template-tag";
import { Observable, pipe, combineLatest } from "rxjs";
import { map, switchMap, tap } from "rxjs/operators";

import { BusinessComponentBase } from "../../../shared/components/business.component.base";
import {
  MessageBriefFragment,
  MessagesGql,
  MessagesQuery,
  MessagesQueryVariables,
  MessageSeverityType,
  CollectionSegmentInfo,
} from "../../../shared/graphql/.generated/type";

@Component({
  selector: "app-messaging-messages",
  templateUrl: "./messages.component.html",
})
export class MessagingMessagesComponent extends BusinessComponentBase {
  data: MessageBriefFragment[];
  selectedData: MessageBriefFragment[];
  searchSchema: SFSchema = {
    properties: {
      title: {
        type: "string",
        title: "标题",
      },
    },
  };
  @ViewChild("sf")
  readonly sf!: SFComponent;
  @ViewChild("st")
  readonly st!: STComponent;
  columns: Array<STColumn<MessageBriefFragment>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    // { title: 'Id', index: 'id' },
    {
      title: "标题",
      index: "title",
      type: "link",
      click: (item: MessageBriefFragment) => this.router.navigate(["view", item.id], { relativeTo: this.route }),
    },
    {
      title: "重要性",
      index: "severity",
      type: "badge",
      badge: {
        INFO: {
          text: "信息",
          color: "default",
        },
      },
    },
    { title: "消息类型", index: "messageType" },
    { title: "发送人", index: "fromUserId" },
    { title: "发送时间", index: "time", type: "date" },
    {
      title: "操作",
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: (item: MessageBriefFragment) => this.router.navigate(["edit", item.id], { relativeTo: this.route }),
        },
      ],
    },
  ];

  constructor(injector: Injector) {
    super(injector);
  }

  async prepare(params: any) {
    let res = await this.apollo
      .query<MessagesQuery, MessagesQueryVariables>({
        query: MessagesGql,
        variables: {
          skip: Number((params.pi - 1) * (this.st?.ps ?? 10)),
          take: Number(params.ps ?? this.st?.ps ?? 10),
          where: {
            title: {
              contains: params.title ?? "",
            },
          },
          includeDetail: false,
        },
      })
      .toPromise();
    this.st.clearStatus();
    this.st.total = res.data.messages.totalCount;
    this.loading = res.loading;
    this.data = res.data.messages.items;
    this.selectedData = [];
  }
  stChange(args: STChange) {
    if (args.type == "pi" || args.type == "ps") {
      this.router.navigate([], { queryParams: { pi: args.pi, ps: args.ps, title: this.sf.value.title } });
    }

    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
    }
  }

  selectChange(data: MessageBriefFragment[]): void {
    //console.log(data);
  }

  batchAudit(auditPassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
}
