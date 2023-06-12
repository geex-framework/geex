import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

import { BookCategoryEditPage } from "./edit/edit.page";
import { BookCategoryListPage } from "./list/list.page";
import { BookCategoryListPageResolve } from "./list/list.resolve";
import { BookCategoryEditPageResolve } from "./edit/edit.resolve";

const routes: Routes = [
  {
    path: "",
    component: BookCategoryListPage,
    resolve: { params: BookCategoryListPageResolve },
    runGuardsAndResolvers: "always",
  },
  {
    path: "edit",
    component: BookCategoryEditPage,
    resolve: { params: BookCategoryEditPageResolve },
    runGuardsAndResolvers: "always",
   },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BookCategoryRoutingModule {}
