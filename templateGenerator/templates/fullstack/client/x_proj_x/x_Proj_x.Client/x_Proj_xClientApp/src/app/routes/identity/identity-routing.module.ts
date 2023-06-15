import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

const routes: GeexRoutes = [
  { path: "user", loadChildren: () => import("./user/user.module").then(m => m.UserModule) },
  { path: "role", loadChildren: () => import("./role/role.module").then(m => m.RoleModule) },
  { path: "org", loadChildren: () => import("./org/org.module").then(m => m.OrgModule) },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class IdentityRoutingModule {}
