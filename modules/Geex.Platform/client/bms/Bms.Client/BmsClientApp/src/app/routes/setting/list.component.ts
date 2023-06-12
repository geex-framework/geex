import { ChangeDetectorRef, Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ApolloQueryResult } from "@apollo/client/core";
import { STChange, STColumn, STComponent } from "@delon/abc/st";
import { ModalHelper, _HttpClient } from "@delon/theme";
import { Apollo } from "apollo-angular";
import html from "html-template-tag";
import { Observable, pipe, combineLatest, of } from "rxjs";
import { delay, map, switchMap, tap } from "rxjs/operators";

import { I18N } from "../../core";
import { BusinessComponentBase } from "../../shared/components/business.component.base";
import { ListDataContext } from "@/app/shared/components/routed-components/routed-list.component.base";
import { RoutedComponent } from "@/app/shared/components/routed-components/routed.component.base";
import {
  SettingBriefFragment,
  SettingsGql,
  SettingsQuery,
  SettingsQueryVariables,
  CollectionSegmentInfo,
  SettingScopeEnumeration,
} from "../../shared/graphql/.generated/type";

export interface SettingListParams {
  pi: number;
  ps: number;
  name?: string;
}

@Component({
  selector: "app-setting-list",
  templateUrl: "./list.component.html",
})
export class SettingListComponent extends RoutedComponent<SettingListParams, ListDataContext<SettingBriefFragment>> {
  async fetchData(): Promise<ListDataContext<SettingBriefFragment>> {
    let params = this.params.value;
    let res = await this.apollo
      .query<SettingsQuery, SettingsQueryVariables>({
        query: SettingsGql,
        variables: {
          input: { filterByName: params.name },
          skip: Number(((params.pi ?? 1) - 1) * (this.st?.ps ?? 10)),
          take: Number(params.ps ?? this.st?.ps ?? 10),
          includeDetail: false,
        },
      })
      .toPromise();
    this.st.clearStatus();
    this.loading = res.loading;
    this.selectedData = [];
    return {
      data: res.data.settings.items,
      total: res.data.settings.totalCount,
    };
  }
  selectedData: SettingBriefFragment[];

  @ViewChild("st")
  readonly st!: STComponent;
  columns: Array<STColumn<SettingBriefFragment>> = [
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
      // type: "link",
      // click: (item: SettingBriefFragment) => this.router.navigate(["view", item.id], { relativeTo: this.route }),
    },
    {
      title: "值",
      index: "value",
      // type: "link",
      // click: (item: SettingBriefFragment) => this.router.navigate(["view", item.id], { relativeTo: this.route }),
    },
    // {
    //   title: '重要性',
    //   index: 'severity',
    //   type: 'badge',
    //   badge: {
    //     INFO: {
    //       text: '信息',
    //       color: 'default',
    //     },
    //   },
    // },
    // { title: '消息类型', index: 'settingType' },
    // { title: '发送人', index: 'fromUserId' },
    // { title: '发送时间', index: 'time', type: 'date' },
    {
      title: "操作",
      fixed: "right",
      width: 200,
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: (item: SettingBriefFragment) => this.router.navigate(["edit", item.name], { relativeTo: this.route }),
        },
      ],
    },
  ];

  constructor(injector: Injector) {
    super(injector);
  }

  async prepare(params: any) {
    await super.prepare(params);
  }

  stChange(args: STChange) {
    if (args.type == "pi" || args.type == "ps") {
      this.router.navigate([], { queryParams: { pi: args.pi, ps: args.ps, name: this.params.value.name } });
    }

    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
    }
  }

  selectChange(data: SettingBriefFragment[]): void {
    //console.log(data);
  }

  batchAudit(auditPassOrCancel: boolean) {}

  add(): void {
    this.router.navigate(["edit"], { relativeTo: this.route });
  }
}
