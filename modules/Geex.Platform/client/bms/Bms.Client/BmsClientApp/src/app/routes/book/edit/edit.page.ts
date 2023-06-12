import { Component, Injector, OnInit } from "@angular/core";
import { FormBuilder, FormControl, Validators } from "@angular/forms";
import { deepCopy } from "@delon/util";
import { isEqual } from "lodash-es";

import { EditDataContext, RoutedEditComponent } from "../../../shared/components/routed-components/routed-edit.component.base";
import {
  BookByIdQuery,
  BookByIdQueryVariables,
  BookByIdGql,
  CreateBooksGql,
  CreateBookInput,
  EditBookInput,
  EditBooksGql,
  Book,
  BookDetailFragment,
  BookCategorysQueryVariables,
  BookCategorysQuery,
  BookCategorysGql,
  BookCategoryBriefFragment,
} from "../../../shared/graphql/.generated/type";
import { EditMode } from "../../../shared/types/common";

type EntityEditablePart = Pick<Book, "name" | "bookCategoryId" | "cover" | "author" | "press" | "publicationDate" | "isbn">;

export type BookEditPageParams = {
  id: string;
  name: string;
};
type BookEditPageContext = EditDataContext<
  BookDetailFragment,
  keyof EntityEditablePart
> & {
  disabled: boolean;
};

@Component({
  selector: "app-book-edit",
  templateUrl: "./edit.page.html",
  styles: [],
})
export class BookEditPage extends RoutedEditComponent<
  BookEditPageParams,
  BookDetailFragment,
  keyof EntityEditablePart
> {
  mode: EditMode;
  context: BookEditPageContext;
  bookCategoryList: BookCategoryBriefFragment[] = [];
  constructor(injector: Injector) {
    super(injector);
  }

  async fetchData() {
    let params = this.params.value;
    const id = params.id;
    this.mode = id ? "edit" : "create";
    let result: BookEditPageContext = {
      id,
      disabled: false,
    };

    this.bookCategoryList = (
      await this.apollo
        .query<BookCategorysQuery, BookCategorysQueryVariables>({
          query: BookCategorysGql,
          variables: {
            input: { name: "" },
            skip: 0,
            take: 999,
          },
        })
        .toPromise()
    ).data.bookCategorys.items;

    let fb: FormBuilder = new FormBuilder();

    let formConfig: { [key in keyof EntityEditablePart]: FormControl };
    if (id) {
      let res = await this.apollo
        .query<BookByIdQuery, BookByIdQueryVariables>({
          query: BookByIdGql,
          variables: {
            id: id,
          },
        })
        .toPromise();
      let entity = res.data.bookById;
      result.entity = entity;
      formConfig = {
        name: new FormControl(entity.name, Validators.required),
        bookCategoryId: new FormControl(entity.bookCategoryId, Validators.required),
        author: new FormControl(entity.author, Validators.required),
        cover: new FormControl(entity.cover, Validators.required),
        press: new FormControl(entity.press, Validators.required),
        publicationDate: new FormControl(entity.publicationDate, Validators.required),
        isbn: new FormControl(entity.isbn, Validators.required),
      };
    } else {
      formConfig = {
        name: new FormControl("", Validators.required),
        bookCategoryId: new FormControl("", Validators.required),
        author: new FormControl("", Validators.required),
        cover: new FormControl("", Validators.required),
        press: new FormControl("", Validators.required),
        publicationDate: new FormControl("", Validators.required),
        isbn: new FormControl("", Validators.required),
      };
    }
    let entityForm = fb.group(formConfig);
    result.entityForm = entityForm;
    result.originalValue = entityForm.value;
    return result;
  }

  async submit(): Promise<void> {
    if (this.mode === "create") {
      await this.apollo
        .mutate({
          mutation: CreateBooksGql,
          variables: {
            input: {
              name: this.context.entityForm.value.name,
              bookCategoryId: this.context.entityForm.value.bookCategoryId,
              author: this.context.entityForm.value.author,
              cover: this.context.entityForm.value.cover,
              press: this.context.entityForm.value.press,
              publicationDate: this.context.entityForm.value.publicationDate,
              isbn: this.context.entityForm.value.isbn,
            } as CreateBookInput,
          },
        })
        .toPromise();
      this.msgSrv.success("添加成功");
      await this.back(true);
    } else {
      if (this.mode === "edit") {
        await this.apollo
          .mutate({
            mutation: EditBooksGql,
            variables: {
              id: this.context.id,
              input: {
                name: this.context.entityForm.value.name,
                bookCategoryId: this.context.entityForm.value.bookCategoryId,
                author: this.context.entityForm.value.author,
                cover: this.context.entityForm.value.cover,
                press: this.context.entityForm.value.press,
                publicationDate: this.context.entityForm.value.publicationDate,
                isbn: this.context.entityForm.value.isbn,
              } as EditBookInput,
            },
          })
          .toPromise();
        this.msgSrv.success("修改成功");
        await this.back(true);
      }
    }
  }
}
