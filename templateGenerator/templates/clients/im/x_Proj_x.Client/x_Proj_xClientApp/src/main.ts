import "./shared/array.extension";
import "./shared/string.extension";
import "./shared/extensions";
import "./shared/angular.extension";
import "./shared/file.extensions";
import "./shared/ng-alain.extension";
import "./shared/ngxs.extensions";
import "./shared/rxjs.extension";
import { HttpEventType, HttpRequest } from "@angular/common/http";
import { enableProdMode, ViewEncapsulation } from "@angular/core";
import { platformBrowserDynamic } from "@angular/platform-browser-dynamic";
import { preloaderFinished } from "@delon/theme";
import json5 from "json5";
import _ from "lodash";
import { NzSafeAny } from "ng-zorro-antd/core/types";

import "./app/shared/utils/window.extensions";
import "./app/shared/utils/angular-oauth2-oidc.extensions";
import { AppModule } from "./app/app.module";
import { environment } from "./environments/environment";

preloaderFinished();

if (environment.production) {
  enableProdMode();
}

(async () => {
  await initServerSideEnvironments();
  platformBrowserDynamic()
    .bootstrapModule(AppModule, {
      defaultEncapsulation: ViewEncapsulation.Emulated,
      preserveWhitespaces: false,
    })
    .then(res => {
      const win = window as NzSafeAny;
      if (win && win.appBootstrap) {
        win.appBootstrap();
      }
      return res;
    })
    .catch(err => console.error(err));
})();

async function initServerSideEnvironments() {
  try {
    let res = await fetch("/assets/appconfig.json");
    let appConfig = json5.parse(await res.text());
    _.merge(environment, appConfig);
    // let res = await this.httpClient.get("/assets/appconfig.jsonc").toPromise();
    // console.log(res);
  } catch (error) {
    alert("初始化应用配置失败, 如有疑问请联系管理员.");
    console.error(error);
  }
}
