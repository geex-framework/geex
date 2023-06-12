import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { SharedModule } from "@shared";

import { BookRoutingModule } from "./book-routing.module";
import { BookEditPage } from "./edit/edit.page";
import { BookListPage } from "./list/list.page";
import { BookViewPage } from "./view/view.page";

@NgModule({
  imports: [SharedModule, CommonModule, FormsModule, BookRoutingModule],
  declarations: [BookListPage, BookEditPage, BookViewPage],
})
export class BookModule {}
