import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { OrgListComponent } from "./list.component";
import { OrgListComponentResolve } from "./list.resolve";

const routes: GeexRoutes = [
  { path: "", component: OrgListComponent, resolve: { params: OrgListComponentResolve }, runGuardsAndResolvers: "always" },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class OrgsRoutingModule {}
