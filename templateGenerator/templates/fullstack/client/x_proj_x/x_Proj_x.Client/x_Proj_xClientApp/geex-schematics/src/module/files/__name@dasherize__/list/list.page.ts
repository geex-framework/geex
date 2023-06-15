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
import { <%= classify(name) %>BriefFragment,
  <%= classify(name) %>sGql,
  <%= classify(name) %>sQuery,
  Delete<%= classify(name) %>sGql,
  <%= classify(name) %>sQueryVariables,
  SortEnumType } from "../../../shared/graphql/.generated/type";
import { <%= classify(name) %>EditPage } from "../edit/edit.page";

export type <%= classify(name) %>ListPageParam = ListPageParams<<%= classify(name) %>BriefFragment> & {
  filterText: string;
};

@Component({
  selector: "app-<%= dasherize(name) %>-list",
  templateUrl: "./list.page.html",
  styles: [],
})
export class <%= classify(name) %>ListPage extends RoutedListComponent<<%= classify(name) %>ListPageParam, <%= classify(name) %>BriefFragment> {
  override async fetchData(): Promise<ListDataContext<Partial<<%= classify(name) %>BriefFragment>>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<<%= classify(name) %>sQuery, <%= classify(name) %>sQueryVariables>({
        query: <%= classify(name) %>sGql,
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
      total: res.data.<%= camelize(name) %>s.totalCount,
      data: deepCopy(res.data.<%= camelize(name) %>s.items),
      columns: [
        {
          title: "",
          width: 30,
          type: "checkbox",
          index: "checked",
          fixed: "left",
          className: ["text-center"],
        },
        { title: "Id", index: "id", width: 200, className: ["text-center"] },
        {
          title: "名称",
          index: "name",
          sort: {
            key: "name",
            default: params.sort?.name,
          },
          // render: "name",
          className: ["text-center"],
        },
        {
          title: "创建时间",
          index: "createdOn",
          sort: {
            key: "createdOn",
            default: params.sort?.createdOn,
          },
          type: "date",
        },
        {
          title: "操作",
          buttons: [
            {
              icon: "edit",
              text: "编辑",
              click: item => this.router.navigate(["edit"], { queryParams: { id: item.id }, relativeTo: this.route }),
              // acl: AppPermission.<%= classify(name) %>MutationEdit<%= classify(name) %>,
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

  override async prepare(params: <%= classify(name) %>ListPageParam) {
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
        mutation: Delete<%= classify(name) %>sGql,
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
