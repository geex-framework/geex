import { Routes } from "@angular/router";

import { UserEditPage } from "./edit.page";
import { UserListPage } from "./list.page";

export const userRoutes: Routes = [
  { path: "", component: UserListPage },
  { path: "list", component: UserListPage },
  { path: "edit", component: UserEditPage },
  { path: "edit/:id", component: UserEditPage },
];
