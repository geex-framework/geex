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
  ReaderBriefFragment,
  ReadersGql,
  ReadersQuery,
  ReadersQueryVariables,
  SortEnumType,
} from "../../../shared/graphql/.generated/type";
import { ReaderEditPage } from "../edit/edit.page";
import { ReaderViewPage } from "../view/view.page";

export type ReaderListPageParam = ListPageParams<ReaderBriefFragment> & {
  filterText: string;
};

@Component({
  selector: "app-reader-list",
  templateUrl: "./list.page.html",
  styles: [],
})
export class ReaderListPage extends RoutedListComponent<ReaderListPageParam, ReaderBriefFragment> {
  override async fetchData(): Promise<ListDataContext<Partial<ReaderBriefFragment>>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<ReadersQuery, ReadersQueryVariables>({
        query: ReadersGql,
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
      total: res.data.readers.totalCount,
      data: deepCopy(res.data.readers.items),
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
          title: "姓名",
          index: "name",
          className: ["text-center"],
        },
        {
          title: "手机号",
          index: "phone",
          className: ["text-center"],
        },
        {
          title: "性别",
          index: "gender",
          className: ["text-center"],
        },
        {
          title: "出生日期",
          index: "birthDate",
          type: "date",
          dateFormat: "yyyy-MM-dd",
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
              icon: "view",
              text: "详情",
              click: async item => {
                let changed: boolean = await this.modal.create(ReaderViewPage, { id: item.id }).toPromise();
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

  override async prepare(params: ReaderListPageParam) {
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
  override async tableChange(args: STChange) {
    return super.tableChange(args);
  }
}
