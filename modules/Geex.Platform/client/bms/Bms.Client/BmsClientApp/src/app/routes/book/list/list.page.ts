import { Component, Injector, OnInit } from "@angular/core";
import { STChange } from "@delon/abc/st";
import { deepCopy } from "@delon/util";
import { Apollo } from "apollo-angular";
import _ from "lodash";
import { take } from "rxjs/operators";

import {
  ListDataContext,
  ListPageParams,
  RoutedListComponent,
} from "../../../shared/components/routed-components/routed-list.component.base";
import {
  BookBriefFragment,
  BooksGql,
  BooksQuery,
  DeleteBooksGql,
  BooksQueryVariables,
  SortEnumType,
} from "../../../shared/graphql/.generated/type";
import { BookEditPage } from "../edit/edit.page";
import { BookViewPage } from "../view/view.page";

export type BookListPageParam = ListPageParams<BookBriefFragment> & {
  filterText: string;
};

@Component({
  selector: "app-book-list",
  templateUrl: "./list.page.html",
  styles: [],
})
export class BookListPage extends RoutedListComponent<BookListPageParam, BookBriefFragment> {
  override async fetchData(): Promise<ListDataContext<Partial<BookBriefFragment>>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<BooksQuery, BooksQueryVariables>({
        query: BooksGql,
        variables: {
          input: { name: params.filterText },
          skip: Number(((params.pi ?? 1) - 1) * 10),
          take: Number(params.ps ?? 10),
          order: params.sort, // { publicationDate: SortEnumType.Desc },
        },
      })
      .toPromise();
    this.loading = res.loading;
    this.selectedData = [];
    return {
      total: res.data.books.totalCount,
      data: deepCopy(res.data.books.items),
      columns: [
        {
          title: "",
          width: 30,
          type: "checkbox",
          index: "checked",
          fixed: "left",
          className: ["text-center"],
        },
        {
          title: "书名",
          index: "name",
          className: ["text-center"],
        },
        {
          title: "分类",
          index: "bookCategory.name",
          className: ["text-center"],
        },
        {
          title: "封面",
          type: "img",
          index: "attachments.url",
          className: ["text-center"],
        },
        {
          title: "作者",
          index: "author",
          className: ["text-center"],
        },
        {
          title: "出版社",
          index: "press",
          className: ["text-center"],
        },
        {
          title: "出版时间",
          index: "publicationDate",
          type: "date",
          dateFormat: "yyyy-MM-dd",
          className: ["text-center"],
          sort: {
            key: "publicationDate",
            default: params.sort?.publicationDate,
          },
        },
        {
          title: "ISBN",
          index: "isbn",
          className: ["text-center"],
        },
        {
          title: "操作",
          buttons: [
            {
              icon: "edit",
              text: "编辑",
              click: item => this.router.navigate(["edit"], { queryParams: { id: item.id }, relativeTo: this.route }),
              // acl: AppPermission.,
            },
            {
              icon: "delete",
              text: "删除",
              pop: {
                title: "是否确认删除?",
              },
              click: item => {
                this.delete(item.id);
              },
              // acl: AppPermission.,
            },
            {
              icon: "view",
              text: "详情",
              click: async item => {
                let changed: boolean = await this.modal.create(BookViewPage, { id: item.id }).toPromise();
                if (changed) {
                  await this.refresh();
                }
              },
              // acl: AppPermission.,
            },
          ],
          className: ["text-center"],
        },
      ],
    };
  }

  override async prepare(params: BookListPageParam) {
    await super.prepare(params);
  }

  constructor(injector: Injector) {
    super(injector);
  }

  filter: string;

  async add() {
    await this.router.navigate(["./edit"], { relativeTo: this.route });
  }

  override async refresh() {
    return super.refresh();
  }

  override async reset() {
    return super.reset();
  }
  async edit(id: string) {
    await this.router.navigate(["./edit"], { queryParams: { id }, relativeTo: this.route });
  }

  async delete(id: string) {
    await this.apollo
      .mutate({
        mutation: DeleteBooksGql,
        variables: {
          ids: [id],
        },
      })
      .toPromise();
    this.msgSrv.success("已删除");
    this.refresh();
  }

  override async tableChange(args: STChange) {
    return super.tableChange(args);
  }
}
