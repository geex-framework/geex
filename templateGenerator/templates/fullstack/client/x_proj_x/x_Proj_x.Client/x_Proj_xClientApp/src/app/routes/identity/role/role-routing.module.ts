import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { RoleEditComponent } from "./edit/edit.component";
import { RoleEditComponentResolve } from "./edit/edit.resolve";
import { RoleListComponent } from "./list.component";
import { RoleResolveGuard } from "./role-search.resolve";

const routes: GeexRoutes = [
  { path: "", component: RoleListComponent, resolve: { params: RoleResolveGuard }, runGuardsAndResolvers: "always" },
  { path: "edit", component: RoleEditComponent, resolve: { params: RoleEditComponentResolve }, runGuardsAndResolvers: "always", data: {} },
  {
    path: "edit/:id",
    component: RoleEditComponent,
    resolve: { params: RoleEditComponentResolve },
    runGuardsAndResolvers: "always",
    data: { title: "编辑" },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class RolesRoutingModule {}
