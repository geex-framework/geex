import { NgModule } from "@angular/core";
import { RouterModule, GeexRoutes } from "@angular/router";

import { UserEditComponent } from "./edit/edit.component";
import { UserListComponent } from "./list.component";
import { UserResolveGuard } from "./user-search.resolve";

const routes: GeexRoutes = [
  { path: "", component: UserListComponent, resolve: { params: UserResolveGuard }, runGuardsAndResolvers: "always" },
  { path: "edit", component: UserEditComponent },
  { path: "edit/:id", component: UserEditComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class UsersRoutingModule {}
