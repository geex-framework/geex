import { Component, Injector, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { _HttpClient } from "@delon/theme";
import { NzMessageService } from "ng-zorro-antd/message";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";

import { BusinessComponentBase } from "../../../../shared/components/business.component.base";
import {
  MessagesQuery,
  MessagesQueryVariables,
  MessagesGql,
  MessageDetailFragment,
  MessageBriefFragment,
} from "../../../../shared/graphql/.generated/type";

@Component({
  selector: "app-messaging-view",
  templateUrl: "./view.component.html",
})
export class MessagingViewComponent extends BusinessComponentBase {
  id: string;
  data: MessageBriefFragment & MessageDetailFragment;

  constructor(injector: Injector) {
    super(injector);
  }

  async prepare(params: any) {
    this.id = params.id;
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
    this.loading = res.loading;
    this.data = res.data.messages.items[0];
  }

  edit() {
    this.router.navigate(["messaging", "edit", this.id]);
  }
  reload() {
    this.router.navigate([], { skipLocationChange: true });
  }
}
