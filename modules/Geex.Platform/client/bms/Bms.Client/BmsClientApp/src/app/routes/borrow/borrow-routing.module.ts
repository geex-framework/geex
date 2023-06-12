import { NgModule } from "@angular/core";
import { RouterModule, Routes } from "@angular/router";

import { BorrowViewPage } from "./view/view.page";
import { BorrowViewPageResolve } from "./view/view.resolve";
const routes: Routes = [
  {
    path: "",
    component: BorrowViewPage,
    resolve: { params: BorrowViewPageResolve },
    runGuardsAndResolvers: "always",
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BorrowRoutingModule {}
