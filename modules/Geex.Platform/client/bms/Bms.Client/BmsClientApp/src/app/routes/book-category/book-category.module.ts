import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { SharedModule } from "@shared";

import { BookCategoryRoutingModule } from "./book-category-routing.module";
import { BookCategoryEditPage } from "./edit/edit.page";
import { BookCategoryListPage } from "./list/list.page";

@NgModule({
  imports: [SharedModule, CommonModule, FormsModule, BookCategoryRoutingModule],
  declarations: [BookCategoryListPage, BookCategoryEditPage],
})
export class BookCategoryModule {}
