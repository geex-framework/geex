import { Routes } from "@angular/router";

import { RoleEditComponent } from "./edit.page";
import { RoleListComponent } from "./list.page";

export const roleRoutes: Routes = [
  { path: "", component: RoleListComponent },
  { path: "edit", component: RoleEditComponent },
  { path: "edit/:id", component: RoleEditComponent },
];
