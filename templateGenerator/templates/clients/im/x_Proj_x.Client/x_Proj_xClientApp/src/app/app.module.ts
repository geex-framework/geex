import { registerLocaleData } from "@angular/common";
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { default as ngLang } from "@angular/common/locales/zh";
import { APP_INITIALIZER, LOCALE_ID, NgModule, Type } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { RouterModule, UrlSerializer } from "@angular/router";
import { GeexTranslateLoader, I18NService, DefaultInterceptor, StartupService } from "@core";
import { SimpleInterceptor } from "@delon/auth";
import { DELON_LOCALE, zh_CN as delonLang, ALAIN_I18N_TOKEN } from "@delon/theme";
import { TranslateModule, TranslateLoader } from "@ngx-translate/core";
import { NgxsReduxDevtoolsPluginModule } from "@ngxs/devtools-plugin";
import { NgxsStoragePluginModule } from "@ngxs/storage-plugin";
import { NgxsModule } from "@ngxs/store";
import { JsonSchemaModule } from "@shared";
import { OAuthModule, OAuthStorage } from "angular-oauth2-oidc";
import { zhCN as dateLang } from "date-fns/locale";
import { NZ_DATE_LOCALE, NZ_I18N, zh_CN as zorroLang } from "ng-zorro-antd/i18n";
import { NzMessageModule } from "ng-zorro-antd/message";
import { NzNotificationModule } from "ng-zorro-antd/notification";

import { environment } from "../environments/environment";
import { AppComponent } from "./app.component";
import { CoreModule } from "./core/core.module";
import { GlobalConfigModule } from "./global-config.module";
import { LayoutModule } from "./layout/layout.module";
import { RoutesModule } from "./routes/routes.module";
import { CacheDataState } from "./shared/states/cache-data.state";
import { TenantState } from "./shared/states/tenant.state";
import { UserDataState } from "./shared/states/user-data.state";

// #region default language
// Reference: https://ng-alain.com/docs/i18n
const LANG = {
  abbr: "zh",
  ng: ngLang,
  zorro: zorroLang,
  date: dateLang,
  delon: delonLang,
};
// register angular
registerLocaleData(LANG.ng, LANG.abbr);
const LANG_PROVIDES = [
  { provide: LOCALE_ID, useValue: LANG.abbr },
  { provide: NZ_I18N, useValue: LANG.zorro },
  { provide: NZ_DATE_LOCALE, useValue: LANG.date },
  { provide: DELON_LOCALE, useValue: LANG.delon },
];
// #endregion

export function storageFactory(): OAuthStorage {
  return localStorage;
}

// #region i18n services

const I18NSERVICE_MODULES = [
  TranslateModule.forRoot({
    loader: {
      provide: TranslateLoader,
      useClass: GeexTranslateLoader,
    },
    defaultLanguage: "en",
    useDefaultLang: true,
  }),
];

const I18NSERVICE_PROVIDES = [{ provide: ALAIN_I18N_TOKEN, useClass: I18NService, multi: false }];
// #region

// #region JSON Schema form (using @delon/form)
const FORM_MODULES = [JsonSchemaModule];
// #endregion

// #region Http Interceptors
const INTERCEPTOR_PROVIDES = [
  { provide: HTTP_INTERCEPTORS, useClass: SimpleInterceptor, multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: DefaultInterceptor, multi: true },
];
// #endregion

// #region global third module
const GLOBAL_THIRD_MODULES: Array<Type<any>> = [];
// #endregion

// #region Startup Service
export function StartupServiceFactory(startupService: StartupService): () => Promise<void> {
  return () => startupService.load();
}
const APPINIT_PROVIDES = [
  StartupService,
  {
    provide: APP_INITIALIZER,
    useFactory: StartupServiceFactory,
    deps: [StartupService],
    multi: true,
  },
  { provide: OAuthStorage, useFactory: storageFactory },
];
// #endregion

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    GlobalConfigModule.forRoot(),
    CoreModule,
    RouterModule,
    FormsModule,
    LayoutModule,
    RoutesModule,
    NzMessageModule,
    OAuthModule.forRoot(),
    NgxsModule.forRoot([], {
      developmentMode: !environment.production,
    }),
    NgxsReduxDevtoolsPluginModule.forRoot({
      maxAge: 25, // Retains last 25 states
      disabled: environment.production,
    }),
    NgxsStoragePluginModule.forRoot({
      key: [UserDataState, TenantState, CacheDataState],
    }),
    // Connects RouterModule with StoreModule, uses MinimalRouterStateSerializer by default
    // NgxsRouterPluginModule.forRoot(),
    NzNotificationModule,
    ...I18NSERVICE_MODULES,
    ...FORM_MODULES,
    ...GLOBAL_THIRD_MODULES,
  ],
  providers: [...LANG_PROVIDES, ...INTERCEPTOR_PROVIDES, ...APPINIT_PROVIDES, ...I18NSERVICE_PROVIDES],
  bootstrap: [AppComponent],
})
export class AppModule {}
