import { inject, Injectable, Injector, NgModule } from "@angular/core";
import {
  RouterModule,
  GeexRoutes,
  GeexRoute,
  Resolve,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  CanActivate,
  UrlTree,
  UrlSerializer,
} from "@angular/router";
import { ACLGuard, ACLService } from "@delon/acl";
import { JWTGuard } from "@delon/auth";
import { environment } from "@env/environment";
import { Observable, of } from "rxjs";

// layout
import { LayoutBasicComponent } from "../layout/basic/basic.component";
import { LayoutBlankComponent } from "../layout/blank/blank.component";
import { AuthorizeGuard } from "../shared/guards/authorize.guard";
import { InitLayoutResolver } from "../shared/resolvers/init-layout.resolver";
import { PreserveQueryParamsResolver } from "../shared/resolvers/preserve-query-params.resolver";

// dashboard pages
const routes: GeexRoutes = [
  {
    path: "",
    component: LayoutBasicComponent,
    canActivate: [AuthorizeGuard],
    children: [
      { path: "", redirectTo: "dashboard", pathMatch: "full" },
      // 业务子模块
      { path: "messaging", loadChildren: () => import("./messaging/messaging.module").then(m => m.MessagingModule) },
      { path: "identity", loadChildren: () => import("./identity/identity.module").then(m => m.IdentityModule) },
      { path: "setting", loadChildren: () => import("./setting/setting.module").then(m => m.SettingModule) },
      { path: "saas", loadChildren: () => import("./saas/saas.module").then(m => m.SaasModule) },
      {
        path: "personal-center",
        loadChildren: () => import("./personal-center/personal-center.module").then(m => m.PersonalCenterModule),
      },
    ],
    resolve: {
      routes: PreserveQueryParamsResolver,
      init: InitLayoutResolver,
    },
    runGuardsAndResolvers: "always",
  },
  // 空白布局
  {
    path: "blank",
    component: LayoutBlankComponent,
    children: [],
  },
  // passport
  {
    path: "passport",
    loadChildren: () => import("./passport/passport.module").then(m => m.PassportModule),
  },
  {
    path: "exception",
    loadChildren: () => import("./exception/exception.module").then(m => m.ExceptionModule),
  },
  { path: "**", redirectTo: "exception/404" },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, {
      useHash: false,
      // enableTracing: !environment.production,
      onSameUrlNavigation: "reload",
      urlUpdateStrategy: "eager",
      canceledNavigationResolution: "computed",
      // NOTICE: If you use `reuse-tab` component and turn on keepingScroll you can set to `disabled`
      // Pls refer to https://ng-alain.com/components/reuse-tab
      // scrollPositionRestoration: 'top',
    }),
  ],
  exports: [RouterModule],
})
export class RouteRoutingModule {}
