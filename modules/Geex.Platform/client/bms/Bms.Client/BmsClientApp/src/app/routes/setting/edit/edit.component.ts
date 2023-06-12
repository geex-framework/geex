import { Location } from "@angular/common";
import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { SFComponent, SFSchema, SFUISchema, SFUploadWidgetSchema } from "@delon/form";
import { _HttpClient } from "@delon/theme";
import { Store } from "@ngxs/store";
import { NzMessageService } from "ng-zorro-antd/message";
import { from, Observable, Subscription } from "rxjs";
import { map, mergeMap } from "rxjs/operators";

import { BusinessComponentBase } from "../../../shared/components/business.component.base";
import {
  EditSettingGql,
  // CreateSettingGql,
  ISetting,
  SettingsGql,
  SettingsQuery,
  SettingsQueryVariables,
  // CreateSettingMutationVariables,
  EditSettingMutationVariables,
  CreateBlobObjectGql,
  BlobStorageType,
  IBlobObject,
  Maybe,
  SettingScopeEnumeration,
  EditSettingRequestInput,
  SettingDefinition,
  ITenant,
} from "../../../shared/graphql/.generated/type";
import { TenantState } from "../../../shared/states/tenant.state";
import { EditMode } from "../../../shared/types/common";

@Component({
  selector: "app-settings-edit",
  templateUrl: "./edit.component.html",
  styles: [
    `
      ::ng-deep .editor {
        height: 600px;
      }
      ::ng-deep .monaco-editor .view-line > span {
        width: 0;
        display: block;
      }
    `,
  ],
})
export class SettingEditComponent extends BusinessComponentBase {
  mode: EditMode;
  name: string;
  data: EditSettingRequestInput;
  @ViewChild("sf")
  readonly sf!: SFComponent;
  schema: SFSchema = {
    properties: {
      name: {
        type: "string",
        title: "定义名称",
        readOnly: true,
        enum: [...Object.entries(SettingDefinition).map(x => x[1])],
      },
      value: {
        type: "string",
        title: "值",
        ui: {
          widget: "code-editor",
          language: "json",
        },
      },
    } as { [key in keyof EditSettingRequestInput]: SFSchema },
    required: ["name" /*'settingType', 'severity'*/],
  };
  ui: SFUISchema = {
    "*": {
      spanLabel: 3,
    },
  };
  tenant: Partial<ITenant>;

  constructor(private injector: Injector) {
    super(injector);
  }

  async prepare(params: any) {
    this.tenant = this.injector.get(Store).selectSnapshot(TenantState);
    this.name = params.name;
    this.mode = params.name == undefined ? "create" : "edit";
    if (params.name) {
      let res = await this.apollo
        .query<SettingsQuery, SettingsQueryVariables>({
          query: SettingsGql,
          variables: {
            input: {},
            where: {
              name: {
                eq: params.name,
              },
            },
            includeDetail: true,
          },
        })
        .toPromise();
      let setting = res.data.settings.items[0];

      this.data = {
        name: setting.name,
        scope: setting.scope,
        scopedKey: setting.scopedKey,
        value: JSON.stringify(setting.value),
      };
    } else {
      this.data = {
        // settingType: SettingType.Notification,
        // severity: SettingSeverityType.Info,
        // time: new Date(),
        name: "" as any,
        // toUserIds: [],
      };
    }
  }

  async submit(value: Partial<EditSettingRequestInput>): Promise<void> {
    if (this.mode == "create") {
      // let res = await this.apollo
      //   .mutate({
      //     mutation: CreateSettingGql,
      //     variables: {
      //       input: value,
      //     },
      //   })
      //   .toPromise();
      // if (res.data.createSetting) {
      //   this.msgSrv.success("创建成功");
      //   this.router.navigate([".."], { relativeTo: this.route, forceReload: true});
      // }
    } else {
      // const originValue = value.value;
      // try {
      //   value.value = JSON.parse(value.value);
      // } catch (error) {
      //   value.value = originValue;
      // }
      let res = await this.apollo
        .mutate({
          mutation: EditSettingGql,
          variables: {
            input: {
              scope: this.tenant?.code ? SettingScopeEnumeration.Tenant : SettingScopeEnumeration.Global,
              scopedKey: this.tenant?.code,
              name: value.name,
              value: value.value,
            },
          },
        })
        .toPromise();
      if (res.data.editSetting) {
        this.msgSrv.success("修改成功");
        await this.router.navigate(["/setting"], { relativeTo: this.route, replaceUrl: true, forceReload: true });
      }
    }
  }
}
