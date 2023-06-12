import { Component, Input, OnInit } from "@angular/core";
import { _HttpClient } from "@delon/theme";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";

import { BookByIdGql, BookByIdQuery, BookByIdQueryVariables, BookDetailFragment } from "../../../shared/graphql/.generated/type";

@Component({
  selector: "app-book-view",
  templateUrl: "./view.page.html",
})
export class BookViewPage implements OnInit {
  record: any = {};
  i: any;
  @Input() id;
  bookEntity: BookDetailFragment;

  constructor(private modal: NzModalRef, private apollo: Apollo) {}

  async ngOnInit(): Promise<void> {
    this.bookEntity = (
      await this.apollo
        .query<BookByIdQuery, BookByIdQueryVariables>({
          query: BookByIdGql,
          variables: {
            id: this.id,
          },
        })
        .toPromise()
    ).data.bookById;
  }

  close(): void {
    this.modal.destroy();
  }
}
