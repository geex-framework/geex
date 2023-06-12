import { CommonModule } from "@angular/common";
import { NgModule, Type } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { SharedModule } from "@shared";

import { BorrowRoutingModule } from "./borrow-routing.module";
import { BorrowViewPage } from "./view/view.page";

const COMPONENTS: Array<Type<void>> = [BorrowViewPage];

@NgModule({
  imports: [SharedModule, CommonModule, FormsModule, BorrowRoutingModule],
  declarations: COMPONENTS,
})
export class BorrowModule {}
