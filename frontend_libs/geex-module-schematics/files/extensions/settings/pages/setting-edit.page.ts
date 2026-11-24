import { Component, ViewChild } from "@angular/core";
import { SFComponent, SFSchema, SFUISchema } from "@delon/form";

import { FormControl } from "@angular/forms";
import { geex, RoutedEditComponent, RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import type { EditSettingRequest } from "@/gql";
import { SettingDefinition } from "@/gql";
import { SettingScopeEnumeration } from "@/gql";
import { settings, editSetting, restoreTenantSettingInheritance } from "../graphql/operations.gql";
import type { SettingBrief, SettingDetail } from "../graphql/operations.gql";
import { NzCodeEditorModule } from "ng-zorro-antd/code-editor";
type SettingEditFormSchema = Pick<EditSettingRequest, "scope" | "scopedKey" | "value">;

@Component({
  selector: "app-settings-edit",
  templateUrl: "./setting-edit.page.html",
  standalone: true,
  styles: [
    `
      ::ng-deep .editor {
        height: 55vh;
      }
      ::ng-deep .monaco-editor .view-line > span {
        width: 0;
        display: block;
      }
    `,
  ],
  imports: [SharedModule, NzCodeEditorModule],
})
export class SettingEditComponent extends RoutedEditComponent<
  { id?: string; name?: SettingDefinition },
  SettingBrief & SettingDetail,
  SettingEditFormSchema
> {
  override async onRouted(params: { name?: SettingDefinition }) {
    this.name = params.name;
    if (params.name) {
      let res = await this.apollo
        .query({
          query: settings,
          variables: {
            request: {},
            filter: {
              name: {
                eq: params.name,
              },
            },
            includeDetail: true,
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      let setting = res.data.settings.items[0];
      let controls = {
        scope: new FormControl(setting.scope),
        scopedKey: new FormControl(setting.scopedKey),
        value: new FormControl(setting.value),
      };
      const entityForm = this.fb.build(controls);
      this.entityForm = entityForm;
      this.entity = setting;
    }
  }
  routeParamsMappings: RouteParamsMappings<{ name?: SettingDefinition }> = {
    name: { position: "pathParams", default: undefined },
  };
  name: SettingDefinition;
  @ViewChild("sf")
  readonly sf!: SFComponent;
  schema: SFSchema = this.buildSchema();
  ui: SFUISchema = {
    "*": {
      spanLabel: 3,
    },
  };

  private buildSchema(): SFSchema {
    return {
      properties: {
        name: {
          type: "string",
          title: this.I18N.Settings.columnName,
          readOnly: true,
        },
        value: {
          type: "string",
          title: this.I18N.Settings.columnValue,
          ui: {
            widget: "code-editor",
            language: "json",
          },
        },
      } as { [key in keyof EditSettingRequest]: SFSchema },
      required: ["name" /*'settingType', 'fullname'*/],
    };
  }

  async submit(value: Partial<EditSettingRequest>): Promise<void> {
    if (!this.entity) return;
    const tenantCode = geex.multiTenant.current()?.code;
    const tenantOverride = tenantCode && this.entity.validScopes.includes(SettingScopeEnumeration.Tenant);
    const result = await this.apollo.mutate({
      mutation: editSetting,
      variables: { request: {
        name: this.entity.name,
        scope: tenantOverride ? SettingScopeEnumeration.Tenant : this.entity.scope,
        scopedKey: tenantOverride ? tenantCode : this.entity.scopedKey,
        value: value.value,
      } },
    }).firstValuePromise();
    if (result.data.editSetting) await this.finishSave();
  }

  async restoreInheritance(): Promise<void> {
    const result = await this.apollo.mutate({
      mutation: restoreTenantSettingInheritance,
      variables: { name: this.name, tenantCode: geex.multiTenant.current()?.code },
    }).firstValuePromise();
    if (result.data.restoreTenantSettingInheritance) await this.finishSave();
  }

  private async finishSave(): Promise<void> {
    this.msgSrv.success(this.I18N.Settings.editSuccess);
    await Promise.all([
      geex.settings.init(true),
      this.router.navigate(["/settings"], { relativeTo: this.route, replaceUrl: true, closeCurrentTab: true, forceReload: true }),
    ]);
  }
}
