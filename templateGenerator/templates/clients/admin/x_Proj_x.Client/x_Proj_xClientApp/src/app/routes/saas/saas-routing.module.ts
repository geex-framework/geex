import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { TenantListComponent } from "./list/list.component";

const routes: GeexRoutes = [{ path: "", component: TenantListComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SaasRoutingModule {}
