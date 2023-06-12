import { Component, Input, OnInit } from "@angular/core";
import { _HttpClient } from "@delon/theme";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";

import { ReaderByIdGql, ReaderByIdQuery, ReaderByIdQueryVariables, ReaderDetailFragment } from "../../../shared/graphql/.generated/type";

@Component({
  selector: "app-reader-view",
  templateUrl: "./view.page.html",
})
export class ReaderViewPage implements OnInit {
  record: any = {};
  i: any;
  @Input() id;
  bookEntity: ReaderDetailFragment;

  constructor(private modal: NzModalRef, private apollo: Apollo) {}

  async ngOnInit(): Promise<void> {
    this.bookEntity = (
      await this.apollo
        .query<ReaderByIdQuery, ReaderByIdQueryVariables>({
          query: ReaderByIdGql,
          variables: {
            id: this.id,
          },
        })
        .toPromise()
    ).data.readerById;
  }

  close(): void {
    this.modal.destroy();
  }
}
