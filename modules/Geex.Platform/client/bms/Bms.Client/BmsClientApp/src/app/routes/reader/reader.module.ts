import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { SharedModule } from "@shared";

import { ReaderEditPage } from "./edit/edit.page";
import { ReaderListPage } from "./list/list.page";
import { ReaderRoutingModule } from "./reader-routing.module";
import { ReaderViewPage } from "./view/view.page";

@NgModule({
  imports: [SharedModule, CommonModule, FormsModule, ReaderRoutingModule],
  declarations: [ReaderListPage, ReaderEditPage, ReaderViewPage],
})
export class ReaderModule {}
