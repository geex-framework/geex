import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

import { BookEditPage } from "./edit/edit.page";
import { BookEditPageResolve } from "./edit/edit.resolve";
import { BookListPage } from "./list/list.page";
import { BookListPageResolve } from "./list/list.resolve";
import { BookViewPage } from "./view/view.page";

const routes: Routes = [
  {
    path: "",
    component: BookListPage,
    resolve: { params: BookListPageResolve },
    runGuardsAndResolvers: "always",
  },
  {
    path: "edit",
    component: BookEditPage,
    resolve: { params: BookEditPageResolve },
    runGuardsAndResolvers: "always",
  },
  {
    path: "view",
    component: BookViewPage,
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BookRoutingModule {}
