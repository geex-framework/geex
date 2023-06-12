import { Component, Injector, OnInit } from "@angular/core";
import { FormBuilder, FormControl, Validators } from "@angular/forms";
import { deepCopy } from "@delon/util";
import { isEqual } from "lodash-es";

import { EditDataContext, RoutedEditComponent } from "../../../shared/components/routed-components/routed-edit.component.base";
import {
  BookCategoryByIdQuery,
  BookCategoryByIdQueryVariables,
  BookCategoryByIdGql,
  CreateBookCategorysGql,
  CreateBookCategoryInput,
  EditBookCategoryInput,
  EditBookCategorysGql,
  BookCategory,
  BookCategoryDetailFragment,
} from "../../../shared/graphql/.generated/type";
import { EditMode } from "../../../shared/types/common";

type EntityEditablePart = Pick<BookCategory, "name" | "describe">;

export type BookCategoryEditPageParams = {
  id: string;
  name: string;
};
type BookCategoryEditPageContext = EditDataContext<BookCategoryDetailFragment, "name" | "describe"> & {
  disabled: boolean;
};

@Component({
  selector: "app-book-category-edit",
  templateUrl: "./edit.page.html",
  styles: [],
})
export class BookCategoryEditPage extends RoutedEditComponent<BookCategoryEditPageParams, BookCategoryDetailFragment, "name" | "describe"> {
  mode: EditMode;
  context: BookCategoryEditPageContext;

  constructor(injector: Injector) {
    super(injector);
  }
  async fetchData() {
    let params = this.params.value;
    const id = params.id;
    this.mode = id ? "edit" : "create";
    let result: BookCategoryEditPageContext = {
      id,
      disabled: false,
    };
    let fb: FormBuilder = new FormBuilder();

    let formConfig: { [key in keyof EntityEditablePart]: FormControl };
    if (id) {
      let res = await this.apollo
        .query<BookCategoryByIdQuery, BookCategoryByIdQueryVariables>({
          query: BookCategoryByIdGql,
          variables: {
            id: id,
          },
        })
        .toPromise();
      let entity = res.data.bookCategoryById;
      result.entity = entity;
      formConfig = {
        name: new FormControl(entity.name, Validators.required),
        describe: new FormControl(entity.describe),
      };
    } else {
      formConfig = {
        name: new FormControl("", Validators.required),
        describe: new FormControl(""),
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
          mutation: CreateBookCategorysGql,
          variables: {
            input: {
              name: this.context.entityForm.value.name,
              describe: this.context.entityForm.value.describe,
            } as CreateBookCategoryInput,
          },
        })
        .toPromise();
      this.msgSrv.success("添加成功");
      await this.back(true);
    } else {
      if (this.mode === "edit") {
        await this.apollo
          .mutate({
            mutation: EditBookCategorysGql,
            variables: {
              id: this.context.id,
              input: {
                name: this.context.entityForm.value.name,
                describe: this.context.entityForm.value.describe,
              } as EditBookCategoryInput,
            },
          })
          .toPromise();
        this.msgSrv.success("修改成功");
        await this.back(true);
      }
    }
  }
}
