# @geexcode/geex-angular

Geex Angular **Core**: headless signal modules + **Delon page bases**.

Delon / ng-zorro are **required peer dependencies** (not an optional side package). Align versions with the admin template (Delon / ng-zorro 20.x).

## Install

```bash
npm install @geexcode/geex-angular
```

Peers (excerpt): `@angular/*`, `@delon/abc|acl|form|theme|util`, `ng-zorro-antd`, `apollo-angular`, `extract-files`, `graphql`, `graphql-ws`, `kiwi-intl`, `rxjs`.

## Providers

```ts
import {
  provideGeexCommon,
  provideGeexDelonBase,
  provideGeexApollo,
  provideGeexI18n,
  provideGeexStartup,
  GeexI18nService,
  GeexStartupService,
  GeexHttpInterceptor,
  GeexTranslateLoader,
  loadEnvironmentOverrides,
  GEEX_APP_PERMISSION,
  GEEX_API_BASE_URL,
  GEEX_AFTER_LOGIN_NAVIGATE,
} from "@geexcode/geex-angular";
import { ALAIN_I18N_TOKEN } from "@delon/theme";

await loadEnvironmentOverrides(environment);

bootstrapApplication(AppComponent, {
  providers: [
    provideGeexCommon(),
    ...provideGeexI18n(hostI18nPacks),
    { provide: ALAIN_I18N_TOKEN, useExisting: GeexI18nService },
    { provide: GEEX_APP_PERMISSION, useValue: AppPermission },
    { provide: GEEX_API_BASE_URL, useValue: environment.api.baseUrl },
    { provide: GEEX_AFTER_LOGIN_NAVIGATE, useValue: () => window.clearHistory() },
    GeexHttpInterceptor,
    { provide: HTTP_INTERCEPTORS, useExisting: GeexHttpInterceptor, multi: true },
    ...provideGeexStartup({ /* getOAuthConfig, defaultMenus, settingKeys, onDebuggerInit */ }),
    provideAppInitializer(() => inject(GeexStartupService).load()),
    ...provideGeexDelonBase({ reuseTab: { /* ... */ } }),
    ...provideGeexApollo({
      baseUrl: environment.api.baseUrl,
      possibleTypes: introspection.possibleTypes,
    }),
  ],
});
```

- `provideGeexCommon()` — headless modules only.
- `provideGeexI18n(packs)` — `GeexI18nService` + `GEEX_I18N` / `GEEX_I18N_PACKS`; host owns zh-CN/en-US dictionary data; also wire `ALAIN_I18N_TOKEN` → `GeexI18nService` and `TranslateLoader` → `GeexTranslateLoader`.
- `provideGeexStartup` / `GeexStartupService` — inject Core service directly (no host facade).
- `provideGeexDelonBase()` — `GeexRouter` + `GeexReuseTabStrategy` + shared tab policy, configured with `reuseTab`.
- `GeexHttpInterceptor` — default zh-CN status messages + `/authentication/login`; override via `GEEX_HTTP_STATUS_MESSAGES` / `GEEX_LOGIN_PATH` / hooks.
- `provideGeexApollo` — default typePolicies + error/silent/WS links + **multipart upload** (`extract-files`); host must pass `baseUrl` + `possibleTypes`. Opt out: `enableUpload: false` or custom `createHttpLinkInstance`.
- `loadEnvironmentOverrides` — merges `/assets/environment.override.js` into host `environment`.

## Apollo cache policy

`provideGeexApollo` defaults `query.fetchPolicy` to `no-cache`: each query reads from the network without writing its result to the Apollo cache. `watchQuery` defaults to `cache-first`. Callers can override either policy per operation.

The default `errorPolicy` is `ignore` for query/watchQuery/mutate. Operations that must reject partial GraphQL errors should explicitly use `errorPolicy: "none"`; the BlobStorage list/create operations apply this policy. See the [BlobStorage contract](../../framework_modules/Geex.Extensions.BlobStorage/README.md) for attachment lookup and reuse behavior.

## Delon surface

| Export | Role |
|--------|------|
| `BusinessComponentBase` / `RoutedComponent` / `RoutedListComponent` / `RoutedEditComponent` | Page bases (Template Method hooks for override) |
| `ModalComponentBase` / `TreeTableComponentBase` | Modal / tree-table bases |
| `GeexRouter` / `GeexReuseTabStrategy` | Router + reuse-tab harden |
| `ListPageLayoutComponent` | Generic list layout |
| `rison` | Query-param encoding |
| `GEEX_*` tokens | Host i18n / permission / HTTP / blob / org / exception injection |

Override: subclass `protected` hooks (`handleRouteReload`, `onTableSort`, `filterBatchIds`, `afterClose`, `getNodeKey`, ...). No Page-Token ladder.

### Tenant type

Hosts should use Core `Tenant` from `@geexcode/geex-angular`. For legacy `ITenant` imports, `export type ITenant = Tenant` is available from the package.

### P2 UI (provide-layer)

| Area | Exports |
|------|---------|
| Approve | `ApproveButtonComponent`, `ApproveBadge`, `GEEX_APPROVE_STATUS_OPTIONS` |
| Permissions | `PermissionTransferWidget`, `PermissionsComponent`, `GEEX_PERMISSION_FILTER` |
| Org tree | `OrgTreeSelectWidget`, `OrgTreeSelectPickerDirective`, `GEEX_ORG_OWNERSHIP_FILTER`, `GEEX_SUPER_ADMIN_USER_ID` |
| Upload | `GeexUploadComponent`, `GeexUploadWidget`, `GEEX_BLOB_*` tokens |
| Guards | `CurrentPlatformRedirectGuard`, `GEEX_MOBILE_PATH_SUFFIX` |
| Exception | `Exception403/404/500Component`, `geexExceptionRoutes`, `provideGeexExceptionRoutes()` |
| Header chrome | `HeaderUserComponent` (`GeexAuthLogout` + `GEEX_PROFILE_PATH` / `GEEX_PROFILE_LABEL`), `HeaderI18nComponent`, fullscreen/search/clear-storage |
| i18n | `GeexI18nService`, `LangObject`, `provideGeexI18n`, `GeexTranslateLoader` |

Host must provide blob GraphQL documents via `GEEX_BLOB_CREATE_DOCUMENT`, `GEEX_BLOB_LIST_DOCUMENT`, `GEEX_BLOB_DELETE_DOCUMENT` (from generated `mutations.gql` / `queries.gql`).

## Host-only (not in Core)

- **debugger-blocker** (product policy + `eval`) — keep in app `shared/services`
- i18n **packs** (`zh-CN` / `en-US` with permissions / module-registry merge) — provide via `provideGeexI18n`
- `environment` values and generated `introspection.possibleTypes`
- optional `clearHistory` via `GEEX_AFTER_LOGIN_NAVIGATE`
- peer `extract-files` installed in host (used by Core default upload link)
- full-page `LayoutBasic` / `LayoutAuth` assembly (logo / watermark / PDF)

Business UI (`modules/*`) and identity widgets stay in the app source.

## 标签路由

`provideGeexDelonBase` 提供共享标签策略. 精确对应菜单的页面默认按实际path复用, 其他页面默认full. 页面可用 `data.tab` 覆盖:

```ts
provideGeexDelonBase({
  reuseTab: { defaults: { menu: "path", other: "full" }, max: 10,
    excludes: [/\/authentication/, /\/exception/] },
});

const routes: GeexRoutes = [
  { path: "org", component: OrgPage },
  { path: "edit/:id", component: EditPage, data: { tab: { reuse: "none" } },
    canDeactivate: [page => page.canLeave()] },
  { path: "details/:id", component: DetailsPage,
    data: { tab: { reuse: "query", queryKeys: ["version"] } } },
];
```

| reuse | 匹配规则 |
| --- | --- |
| none | 当前临时标签, 成功离开后移除并销毁组件 |
| path | 实际路径, 包括不同的路径参数值 |
| query | 实际路径和queryKeys; 省略queryKeys时包含所有查询参数 |
| full | 实际路径/所有查询参数/fragment |

查询参数名称排序后生成标签key, 完整URL独立记录最近一次成功导航. 菜单默认规则只作用于精确菜单目标, 编辑/详情子路径按各自规则处理.

布局接入 `GeexTabMenuDirective` 和 `GeexTabsDirective`:

```html
<layout-default-nav geexTabMenu></layout-default-nav>
<reuse-tab geexTabs #tabs></reuse-tab>
<router-outlet (activate)="tabs.activate($event)" (attach)="tabs.activate($event)"></router-outlet>
```

```ts
private readonly tabs = inject(GeexTabService);

this.tabs.open(["/identity/user/edit", user.id]);
await this.tabs.closeCurrent({ fallbackUrl: "/identity/user", refreshOrigin: true });
```

`open`记录来源完整URL到history.state. `closeCurrent`在守卫允许并成功返回后移除当前标签, `refreshOrigin`使来源页面重新加载. 直接访问没有来源的页面时使用fallbackUrl. `openMenu`的纯路径入口恢复已有标签最新URL, 传入query/fragment时使用指定地址. 普通Router导航及浏览器前进/后退按请求地址执行.

返回固定业务目标的入口可用 `router.navigate(commands, { closeCurrentTab: true, replaceUrl: true, forceReload: true })`. 关闭意图与浏览器历史替换分别配置. 带canDeactivate且未显式设置runGuardsAndResolvers的路由统一使用always, 涵盖query/fragment切换. 主应用使用urlUpdateStrategy: "deferred"和canceledNavigationResolution: "computed", 地址在守卫通过后提交, 浏览器历史由Angular管理.

扩展标题解析时继承 `GeexReuseTabService`. 组件的title更新使用Router当前地址确定归属; 缓存页面仅在活动且参数变化或明确forceReload时重新加载.
