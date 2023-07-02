import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { PersonalCenterListComponent } from "./list.component";

const routes: GeexRoutes = [{ path: "", component: PersonalCenterListComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PersonalCenterRoutingModule {}
