import { Component, Injector, OnInit } from "@angular/core";
import { FormBuilder, FormControl } from "@angular/forms";
import { deepCopy } from "@delon/util";
import { isEqual } from "lodash-es";

import { EditDataContext, RoutedEditComponent } from "../../../shared/components/routed-components/routed-edit.component.base";
import {
  <%= classify(name) %>ByIdQuery,
  <%= classify(name) %>ByIdQueryVariables,
  <%= classify(name) %>ByIdGql,
  Create<%= classify(name) %>sGql,
  Create<%= classify(name) %>Input,
  Edit<%= classify(name) %>Input,
  Edit<%= classify(name) %>sGql,
  <%= classify(name) %>,
  <%= classify(name) %>DetailFragment,
} from "../../../shared/graphql/.generated/type";
import { EditMode } from "../../../shared/types/common";

type EntityEditablePart = Pick<<%= classify(name) %>, "name">;

export type <%= classify(name) %>EditPageParams = {
  id: string;
  name: string;
};
type <%= classify(name) %>EditPageContext = EditDataContext<<%= classify(name) %>DetailFragment, keyof EntityEditablePart> & {
  disabled: boolean;
};

@Component({
  selector: "app-<%= dasherize(name) %>-edit",
  templateUrl: "./edit.page.html",
  styles: [],
})
export class <%= classify(name) %>EditPage extends RoutedEditComponent<<%= classify(name) %>EditPageParams, <%= classify(name) %>DetailFragment, keyof EntityEditablePart> {
  mode: EditMode;
  context: <%= classify(name) %>EditPageContext;

  constructor(injector: Injector) {
    super(injector);
  }

  override close() {
    return super.close();
  }

  async fetchData() {
    let params = this.params.value;
    const id = params.id;
    this.mode = id ? "edit" : "create";
    let result: <%= classify(name) %>EditPageContext={
      id,
      disabled: false,
    }
    let fb: FormBuilder = new FormBuilder();

    let formConfig: { [key in keyof EntityEditablePart]: FormControl };
    if (id) {
      let res = await this.apollo
        .query<<%= classify(name) %>ByIdQuery, <%= classify(name) %>ByIdQueryVariables>({
          query: <%= classify(name) %>ByIdGql,
          variables: {
            id: id,
          },
        })
        .toPromise();
      let entity = res.data.<%= camelize(name) %>ById;
      result.entity = entity;
      formConfig = { name: new FormControl(entity.name) };
    } else {
      formConfig = { name: new FormControl("") };
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
          mutation: Create<%= classify(name) %>sGql,
          variables: {
            input: {
              name: this.context.entityForm.value.name,
            } as Create<%= classify(name) %>Input,
          },
        })
        .toPromise();
      this.msgSrv.success("添加成功");
      await this.back(true);
    } else {
      if (this.mode === "edit") {
        await this.apollo
          .mutate({
            mutation: Edit<%= classify(name) %>sGql,
            variables: {
              id: this.context.id,
              input: {
                name: this.context.entityForm.value.name,
              } as Edit<%= classify(name) %>Input,
            },
          })
          .toPromise();
        this.msgSrv.success("修改成功");
        await this.back(true);
      }
    }
  }
}
