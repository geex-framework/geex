import { Component, Injector, OnInit } from "@angular/core";
import { STChange } from "@delon/abc/st";
import { _HttpClient } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalRef } from "ng-zorro-antd/modal";

import { EditDataContext } from "../../../shared/components/routed-components/routed-edit.component.base";
import {
  ListDataContext,
  ListPageParams,
  RoutedListComponent,
} from "../../../shared/components/routed-components/routed-list.component.base";
import { RoutedComponent } from "../../../shared/components/routed-components/routed.component.base";
import {
  BorrowRecordBriefFragment,
  BorrowRecordsGql,
  BorrowRecordsQuery,
  BorrowRecordsQueryVariables,
  CreateBookCategoryInput,
  CreateBorrowRecordInput,
  CreateBorrowRecordsGql,
  EditBorrowRecordInput,
  EditBorrowRecordsGql,
} from "../../../shared/graphql/.generated/type";

export type BorrowViewPageParams = ListPageParams<BorrowRecordBriefFragment> & {
  userPhone: string;
  bookISBN: string;
};
@Component({
  selector: "app-borrow-view",
  templateUrl: "./view.page.html",
})
export class BorrowViewPage extends RoutedListComponent<BorrowViewPageParams, BorrowRecordBriefFragment> {
  constructor(injector: Injector) {
    super(injector);
  }
  override async fetchData(): Promise<ListDataContext<Partial<BorrowRecordBriefFragment>>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<BorrowRecordsQuery, BorrowRecordsQueryVariables>({
        query: BorrowRecordsGql,
        variables: {
          input: { bookId: "" },
          skip: 0,
          take: 999,
        },
      })
      .toPromise();
    this.loading = res.loading;
    return {
      data: deepCopy(res.data.borrowRecords.items),
      total: res.data.borrowRecords.totalCount,
      columns: [
        {
          title: "书名",
          index: "book.name",
          className: ["text-center"],
        },
        {
          title: "ISBN",
          index: "book.isbn",
          className: ["text-center"],
        },
        {
          title: "借阅人",
          index: "reader.name",
          className: ["text-center"],
        },
        {
          title: "手机号",
          index: "reader.phone",
          className: ["text-center"],
        },
        {
          title: "借阅时间",
          index: "readersDate",
          type: "date",
          dateFormat: "yyyy-MM-dd",
          className: ["text-center"],
        },
        {
          title: "还书时间",
          index: "returnDate",
          type: "date",
          dateFormat: "yyyy-MM-dd",
          className: ["text-center"],
        },
      ],
    };
  }

  async create() {
    await this.apollo
      .mutate({
        mutation: CreateBorrowRecordsGql,
        variables: {
          input: {
            userPhone: this.params.controls.userPhone.value,
            bookISBN: this.params.controls.bookISBN.value,
          } as CreateBorrowRecordInput,
        },
      })
      .toPromise();
    this.msgSrv.success("借阅成功");
    this.refresh();
  }

  async edit() {
    await this.apollo
      .mutate({
        mutation: EditBorrowRecordsGql,
        variables: {
          input: {
            userPhone: this.params.controls.userPhone.value,
            bookISBN: this.params.controls.bookISBN.value,
          } as EditBorrowRecordInput,
        },
      })
      .toPromise();
    this.msgSrv.success("借阅成功");
    this.refresh();
  }

  override async refresh() {
    return super.refresh();
  }

  override async reset() {
    return super.reset();
  }

  override async tableChange(args: STChange) {
    return super.tableChange(args);
  }
}
