import { Component, Injector, OnInit } from "@angular/core";
import { FormBuilder, FormControl, Validators } from "@angular/forms";
import { deepCopy } from "@delon/util";
import { isEqual } from "lodash-es";

import { EditDataContext, RoutedEditComponent } from "../../../shared/components/routed-components/routed-edit.component.base";
import {
  ReaderByIdQuery,
  ReaderByIdQueryVariables,
  ReaderByIdGql,
  CreateReadersGql,
  CreateReaderInput,
  EditReaderInput,
  EditReadersGql,
  Reader,
  ReaderDetailFragment,
} from "../../../shared/graphql/.generated/type";
import { EditMode } from "../../../shared/types/common";

type EntityEditablePart = Pick<Reader, "name" | "phone" | "gender" | "birthDate">;

export type ReaderEditPageParams = {
  id: string;
  name: string;
};
type ReaderEditPageContext = EditDataContext<ReaderDetailFragment, "name" | "phone" | "gender" | "birthDate"> & {
  disabled: boolean;
};

@Component({
  selector: "app-reader-edit",
  templateUrl: "./edit.page.html",
  styles: [],
})
export class ReaderEditPage extends RoutedEditComponent<
  ReaderEditPageParams,
  ReaderDetailFragment,
  "name" | "phone" | "gender" | "birthDate"
> {
  mode: EditMode;
  context: ReaderEditPageContext;

  constructor(injector: Injector) {
    super(injector);
  }

  async fetchData() {
    let params = this.params.value;
    const id = params.id;
    this.mode = id ? "edit" : "create";
    let result: ReaderEditPageContext = {
      id,
      disabled: false,
    };
    let fb: FormBuilder = new FormBuilder();

    let formConfig: { [key in keyof EntityEditablePart]: FormControl };
    if (id) {
      let res = await this.apollo
        .query<ReaderByIdQuery, ReaderByIdQueryVariables>({
          query: ReaderByIdGql,
          variables: {
            id: id,
          },
        })
        .toPromise();
      let entity = res.data.readerById;
      result.entity = entity;
      formConfig = {
        name: new FormControl(entity.name, Validators.required),
        phone: new FormControl(entity.phone, Validators.required),
        gender: new FormControl(entity.gender, Validators.required),
        birthDate: new FormControl(entity.birthDate, Validators.required),
      };
    } else {
      formConfig = {
        name: new FormControl("", Validators.required),
        phone: new FormControl("", Validators.required),
        gender: new FormControl("", Validators.required),
        birthDate: new FormControl("", Validators.required),
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
          mutation: CreateReadersGql,
          variables: {
            input: {
              name: this.context.entityForm.value.name,
              gender: this.context.entityForm.value.gender,
              birthDate: this.context.entityForm.value.birthDate,
              phone: this.context.entityForm.value.phone,
            } as CreateReaderInput,
          },
        })
        .toPromise();
      this.msgSrv.success("添加成功");
      await this.back(true);
    } else {
      if (this.mode === "edit") {
        await this.apollo
          .mutate({
            mutation: EditReadersGql,
            variables: {
              id: this.context.id,
              input: {
                name: this.context.entityForm.value.name,
                gender: this.context.entityForm.value.gender,
                birthDate: this.context.entityForm.value.birthDate,
                phone: this.context.entityForm.value.phone,
              } as EditReaderInput,
            },
          })
          .toPromise();
        this.msgSrv.success("修改成功");
        await this.back(true);
      }
    }
  }
}
