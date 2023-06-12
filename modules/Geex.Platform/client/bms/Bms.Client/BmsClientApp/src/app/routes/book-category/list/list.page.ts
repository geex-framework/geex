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
  BookCategoryBriefFragment,
  BookCategorysGql,
  BookCategorysQuery,
  DeleteBookCategorysGql,
  BookCategorysQueryVariables,
  SortEnumType,
} from "../../../shared/graphql/.generated/type";
import { BookCategoryEditPage } from "../edit/edit.page";

export type BookCategoryListPageParam = ListPageParams<BookCategoryBriefFragment> & {
  filterText: string;
};

@Component({
  selector: "app-book-category-list",
  templateUrl: "./list.page.html",
  styles: [],
})
export class BookCategoryListPage extends RoutedListComponent<BookCategoryListPageParam, BookCategoryBriefFragment> {
  override async fetchData(): Promise<ListDataContext<Partial<BookCategoryBriefFragment>>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<BookCategorysQuery, BookCategorysQueryVariables>({
        query: BookCategorysGql,
        variables: {
          input: { name: params.filterText },
          skip: Number(((params.pi ?? 1) - 1) * 10),
          take: Number(params.ps ?? 10),
          order: params.sort,
        },
      })
      .toPromise();
    this.loading = res.loading;
    this.selectedData = [];
    return {
      total: res.data.bookCategorys.totalCount,
      data: deepCopy(res.data.bookCategorys.items),
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
          title: "名称",
          index: "name",
          // render: "name",
          className: ["text-center"],
        },
        {
          title: "描述",
          index: "describe",
          // render: "name",
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
          ],
          className: ["text-center"],
        },
      ],
    };
  }

  override async prepare(params: BookCategoryListPageParam) {
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
        mutation: DeleteBookCategorysGql,
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
