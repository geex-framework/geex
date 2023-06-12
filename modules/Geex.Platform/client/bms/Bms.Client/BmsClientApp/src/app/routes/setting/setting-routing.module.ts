import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";
import { ACLGuard } from "@delon/acl";

import { SettingEditComponent } from "./edit/edit.component";
import { SettingListComponent } from "./list.component";
import { SettingListResolve } from "./list.resolve";

const routes: GeexRoutes = [
  {
    path: "",
    component: SettingListComponent,
    runGuardsAndResolvers: "always",
    resolve: {
      params: SettingListResolve,
    },
  },
  { path: "edit", component: SettingEditComponent, data: {} },
  { path: "edit/:name", component: SettingEditComponent, data: {} },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SettingsRoutingModule {}
