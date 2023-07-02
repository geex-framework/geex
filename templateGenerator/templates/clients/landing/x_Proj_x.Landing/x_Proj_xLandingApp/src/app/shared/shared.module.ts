import { CommonModule } from "@angular/common";
import { HTTP_INTERCEPTORS } from "@angular/common/http";
import { Injector, ModuleWithProviders, NgModule, Type } from "@angular/core";
import { ReactiveFormsModule, FormsModule } from "@angular/forms";
import { RouterModule, ActivatedRouteSnapshot, UrlSerializer } from "@angular/router";
import { STWidgetRegistry } from "@delon/abc/st";
import { ACLService, DelonACLModule } from "@delon/acl";
import { DelonAuthModule, SimpleInterceptor } from "@delon/auth";
import { DelonFormModule, WidgetRegistry } from "@delon/form";
import { AlainThemeModule, MenuService } from "@delon/theme";
import { AlainConfigService } from "@delon/util";
import { NgxsModule, Store } from "@ngxs/store";
import { NgxEchartsModule } from "ngx-echarts";
import { NgxTinymceModule } from "ngx-tinymce";
import { UEditorModule } from "ngx-ueditor";
import { filter, take } from "rxjs/operators";

import { AuditButtonComponent } from "./components/audit-button.component";
import { GeexUploadComponent } from "./components/geex-upload/geex-upload.component";
import { InputPercentComponent } from "./components/input-percent/input-percent.component";
import { PermissionsComponent } from "./components/permissions/permissions.component";
import { StaticChartComponent } from "./components/static-chart/static-chart.component";
import { TemplateComponent, RenderTemplateComponent } from "./components/template.component";
import { WangEditorComponent } from "./components/wangEditor.component";
import { OrgTreeSelectPickerDirective } from "./directives/org-tree-select-picker.directive";
import { ForbiddenValidatorDirective } from "./directives/password-confirm.directive";
import { UserSelectDirective } from "./directives/user-select.directive";
import { GraphQLModule } from "./graphql/graphql.module";
import { LocalStorageACLService } from "./services/localStorageAcl.service";
import { SHARED_DELON_MODULES } from "./shared-delon.module";
import { SHARED_ZORRO_MODULES } from "./shared-zorro.module";
import { CacheDataState, CacheDataState$ } from "./states/cache-data.state";
import { TenantState } from "./states/tenant.state";
import { UserDataState, UserDataState$ } from "./states/user-data.state";
import { GeexUploadWidget } from "./widgets/geex-upload.widget";
import { NumberWidget } from "./widgets/number.widget";
import { OrgTreeSelectWidget } from "./widgets/org-tree-select.widget";
import { PermissionTransferWidget } from "./widgets/permission-transfer.widget";
import { RoleTransferWidget } from "./widgets/role-transfer.widget";
import { STSwitchWidget } from "./widgets/switch.widget";
import { UeditorWidget } from "./widgets/text-editor.widget";
import { UserSelectWidget } from "./widgets/user-select.widget";
import { STYnExportWidget } from "./widgets/yn-export.widget";

// #region third libs
const THIRDMODULES: Array<Type<any>> = [GraphQLModule];

// #endregion

// #region your componets & directives

const COMPONENTS: Array<Type<any>> = [
  StaticChartComponent,
  WangEditorComponent,
  InputPercentComponent,
  GeexUploadComponent,
  RenderTemplateComponent,
  TemplateComponent,
  AuditButtonComponent,
  PermissionsComponent,
];
const DIRECTIVES: Array<Type<any>> = [OrgTreeSelectPickerDirective, ForbiddenValidatorDirective, UserSelectDirective];
const PIPES: Array<Type<any>> = [];
const STWIDGET_COMPONENTS = [
  STSwitchWidget,
  STYnExportWidget,
  NumberWidget,
  GeexUploadWidget,
  OrgTreeSelectWidget,
  RoleTransferWidget,
  PermissionTransferWidget,
  UeditorWidget,
  UserSelectWidget,
];
@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ReactiveFormsModule,
    AlainThemeModule.forChild(),
    DelonACLModule,
    DelonAuthModule,
    DelonFormModule,
    NgxsModule.forFeature([CacheDataState, UserDataState, TenantState]),
    // NgxTinymceModule,
    NgxTinymceModule.forRoot({
      baseURL: "//cdnjs.cloudflare.com/ajax/libs/tinymce/5.7.1/",
    }),
    // UEditorModule.forRoot({
    //   // **注：** 建议使用本地路径；以下为了减少 ng-alain 脚手架的包体大小引用了CDN，可能会有部分功能受影响
    //   js: [`//apps.bdimg.com/libs/ueditor/1.4.3.1/ueditor.config.js`, `//apps.bdimg.com/libs/ueditor/1.4.3.1/ueditor.all.min.js`],
    //   options: {
    //     UEDITOR_HOME_URL: `//apps.bdimg.com/libs/ueditor/1.4.3.1/`,
    //   },
    // }),
    ...SHARED_DELON_MODULES,
    ...SHARED_ZORRO_MODULES,
    // third libs
    ...THIRDMODULES,
    NgxEchartsModule.forRoot({
      echarts: () => import("echarts"), // or import('./path-to-my-custom-echarts')
    }),
  ],
  providers: [
    {
      provide: CacheDataState$,
      useFactory: (store: Store) =>
        store.select(CacheDataState).pipe(
          filter(x => x.cacheInit),
          take(1),
        ),
      deps: [Store],
    },
    // { provide: TenantState$, useFactory: (store: Store) => store.select(Tenant).pipe(take(1)), deps: [Store] },
    { provide: UserDataState$, useFactory: (store: Store) => store.select(UserDataState).pipe(take(1)), deps: [Store] },
    { provide: HTTP_INTERCEPTORS, useClass: SimpleInterceptor, multi: true },
    {
      // LocalStorageACLService对ACLService扩展
      provide: ACLService,
      useFactory: (injector: Injector) => LocalStorageACLService.new(injector),
      deps: [Injector],
    },
  ],
  declarations: [...COMPONENTS, ...DIRECTIVES, ...PIPES, ...STWIDGET_COMPONENTS],
  exports: [
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    AlainThemeModule,
    DelonACLModule,
    DelonFormModule,
    NgxEchartsModule,
    ...SHARED_DELON_MODULES,
    ...SHARED_ZORRO_MODULES,
    ...STWIDGET_COMPONENTS,
    // third libs
    ...THIRDMODULES,
    // your components
    ...COMPONENTS,
    ...DIRECTIVES,
    ...PIPES,
  ],
})
export class SharedModule {
  constructor(widgetRegistry: WidgetRegistry, stWidgetRegistry: STWidgetRegistry) {
    stWidgetRegistry.register(STYnExportWidget.KEY, STYnExportWidget);
    stWidgetRegistry.register(STSwitchWidget.KEY, STSwitchWidget);
    widgetRegistry.register(NumberWidget.KEY, NumberWidget);
    // widgetRegistry.register(AddableListComponent.KEY, AddableListComponent);
    widgetRegistry.register(RoleTransferWidget.KEY, RoleTransferWidget);
    widgetRegistry.register(PermissionTransferWidget.KEY, PermissionTransferWidget);
    widgetRegistry.register(OrgTreeSelectWidget.KEY, OrgTreeSelectWidget);
    widgetRegistry.register(GeexUploadWidget.KEY, GeexUploadWidget);
    widgetRegistry.register(UeditorWidget.KEY, UeditorWidget);
    widgetRegistry.register(UserSelectWidget.KEY, UserSelectWidget);
  }
}
