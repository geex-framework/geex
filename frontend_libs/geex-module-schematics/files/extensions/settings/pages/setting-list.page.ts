import { Component, Injector } from "@angular/core";
import { STColumn } from "@delon/abc/st";
import { _HttpClient } from "@delon/theme";

import { RoutedListComponent } from "@geexcode/geex-angular";
import { RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import { ListPageLayoutComponent } from "@geexcode/geex-angular";
import { settings } from "../graphql/operations.gql";
import type { SettingBrief } from "../graphql/operations.gql";

export interface SettingListParams {
  pi: number;
  ps: number;
  name?: string;
}

@Component({
  selector: "app-setting-list",
  templateUrl: "./setting-list.page.html",
  standalone: true,
  imports: [SharedModule, ListPageLayoutComponent],
})
export class SettingListComponent extends RoutedListComponent<SettingListParams, SettingBrief> {
  override columns?: Array<STColumn<SettingBrief>> = [
    {
      title: "",
      width: 30,
      type: "checkbox",
      index: "checked",
      fixed: "left",
      className: ["text-center"],
    },
    {
      title: "定义名称",
      width: "10%",
      index: "name",
      format: item => {
        return this.I18N.Settings.settingDefinition.get(item.name);
      },
    },
    {
      title: "值",
      index: "value",
      format(item, col, index) {
        const itemValueJsonString = JSON.stringify(item.value, null, 2);
        if (itemValueJsonString.length <= 100) {
          return itemValueJsonString;
        }
        return itemValueJsonString.substring(0, 100) + "...";
      },
    },
    {
      title: "操作",
      width: 200,
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: (item: SettingBrief) => this.router.navigate(["edit", item.name], { relativeTo: this.route }),
        },
      ],
    },
  ];
  override routeParamsMappings: RouteParamsMappings<SettingListParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
    name: { position: "queryParams", default: "" },
  };
  override async onRouted(params: SettingListParams) {
    this.title.set("Settings管理");
    let res = await this.apollo
      .query<any>({
        query: settings,
        variables: {
          request: { filterByName: params.name },
          skip: (params.pi - 1) * params.ps,
          take: params.ps,
          includeDetail: false,
        },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    this.selectedData.set([]);
    this.data.set(res.data["settings"].items);
    this.total.set(res.data["settings"].totalCount);
  }

  selectChange(data: SettingBrief[]): void {
    //console.log(data);
  }

  batchApprove(approvePassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
}
