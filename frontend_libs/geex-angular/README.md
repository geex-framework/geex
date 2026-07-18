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
    provideReuseTabConfig({ /* ... */ }),
    ...provideGeexDelonBase(),
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
- `provideGeexDelonBase()` — `GeexRouter` + `GeexReuseTabStrategy` (call after `provideReuseTabConfig`).
- `GeexHttpInterceptor` — default zh-CN status messages + `/auth/login`; override via `GEEX_HTTP_STATUS_MESSAGES` / `GEEX_LOGIN_PATH` / hooks.
- `provideGeexApollo` — default typePolicies + error/silent/WS links + **multipart upload** (`extract-files`); host must pass `baseUrl` + `possibleTypes`. Opt out: `enableUpload: false` or custom `createHttpLinkInstance`.
- `loadEnvironmentOverrides` — merges `/assets/environment.override.js` into host `environment`.

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
