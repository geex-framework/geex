import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

import { ReaderEditPage } from "./edit/edit.page";
import { ReaderListPage } from "./list/list.page";
import { ReaderListPageResolve } from "./list/list.resolve";
import { ReaderEditPageResolve } from "./edit/edit.resolve";

const routes: Routes = [
  {
    path: "",
    component: ReaderListPage,
    resolve: { params: ReaderListPageResolve },
    runGuardsAndResolvers: "always",
  },
  {
    path: "edit",
    component: ReaderEditPage,
    resolve: { params: ReaderEditPageResolve },
    runGuardsAndResolvers: "always",
   },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ReaderRoutingModule {}
