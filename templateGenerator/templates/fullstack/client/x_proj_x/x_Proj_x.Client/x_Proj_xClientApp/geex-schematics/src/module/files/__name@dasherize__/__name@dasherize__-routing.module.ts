import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

import { <%= classify(name) %>EditPage } from "./edit/edit.page";
import { <%= classify(name) %>ListPage } from "./list/list.page";
import { <%= classify(name) %>ListPageResolve } from "./list/list.resolve";
import { <%= classify(name) %>EditPageResolve } from "./edit/edit.resolve";

const routes: Routes = [
  {
    path: "",
    component: <%= classify(name) %>ListPage,
    resolve: { params: <%= classify(name) %>ListPageResolve },
    runGuardsAndResolvers: "always",
  },
  {
    path: "edit",
    component: <%= classify(name) %>EditPage,
    resolve: { params: <%= classify(name) %>EditPageResolve },
    runGuardsAndResolvers: "always",
   },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class <%= classify(name) %>RoutingModule {}
