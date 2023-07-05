import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup } from "@angular/forms";
import { STChange, STColumn } from "@delon/abc/st";
import { gql } from "apollo-angular";
import { isDate } from "lodash-es";
import { combineLatest, forkJoin, zip } from "rxjs";
import { filter, map } from "rxjs/operators";

import { AuditStatus, Role, SortEnumType } from "../../graphql/.generated/type";
import { EditDataContext, RoutedEditComponent } from "./routed-edit.component.base";
import { MyFormGroup, RoutedComponent } from "./routed.component.base";

export type BatchOperationName = "delete" | "audit" | "submit" | "unaudit" | "unsubmit";

/**数据上下文 */
export class ListDataContext<T extends { id?: string }> {
  data?: Array<Partial<T>>;
  columns?: Array<STColumn<Partial<T>>>;
  total?: number;
}

export class ListPageParams<T> {
  pi?: number;
  ps?: number;
  sort?: { [key in keyof T]?: SortEnumType };
}

type ListDataContextFactory<TDto extends { id?: string }> = (dto: TDto) => ListDataContext<TDto>;

type InferredListDataContext<TDto> = ReturnType<ListDataContextFactory<TDto>>;

/**
 *
 * @class 路由组件基类
 * @extends {BusinessComponentBase}
 * @template TParams 参数对应的表单类型
 * @template TContext 绑定数据上下文类型
 */
@Component({ template: "" })
export abstract class RoutedListComponent<
  TParams extends ListPageParams<TDto>,
  TDto extends { id?: string },
  TContext extends InferredListDataContext<TDto> = InferredListDataContext<TDto>, // 设置默认类型参数
> extends RoutedComponent<TParams, TContext> {
  selectedData: Array<Partial<TDto>> = [];
  override params: MyFormGroup<TParams>;

  public get allSelected() {
    return this.selectedData.length > 0 && this.context?.data?.length == this.selectedData.length;
  }

  // 全选 checkbox 状态改变
  onAllChecked(value: boolean): void {
    this.selectedData = value ? this.context.data : [];
  }

  // tr/td checkbox的勾选状态改变
  onItemChecked(data: Partial<TDto>, checked: boolean): void {
    if (checked) {
      this.selectedData.push(data);
    } else {
      this.selectedData = this.selectedData.filter(x => x.id !== data.id);
    }
  }

  async tableChange(args: STChange) {
    if (args.type == "checkbox") {
      this.selectedData = args.checkbox;
      console.log(this.selectedData);
      return;
    }

    if (args.type == "pi" || args.type == "ps") {
      if (args.pi !== this.params.value.pi || args.ps !== this.params.value.ps) {
        this.params.patchValue({ pi: args.pi, ps: args.ps });
        await this.router.navigate([], { queryParams: this.params.value });
      }
    }

    if (args.sort?.column?.index) {
      let thisSortName = args.sort.column["indexKey"];
      let sorts = args.sort.map["sort"].split("-").map(x => x.split("."));
      let thisSort = sorts.find(x => x[0] == thisSortName);
      if (thisSort) {
        sorts.remove(thisSort);
      }
      sorts.push(thisSort);
      sorts = sorts.where(x => x != undefined && x[0] != "");
      let sortsForm = new FormGroup(Object.fromEntries(sorts.map(x => [x[0], new FormControl(x[1])])));
      this.params.setControl("sort", sortsForm);
      console.log(this.params.value);
      await this.router.navigate([], { queryParams: this.params.value });
    }
    console.log(args);
  }

  batchOperation(sth: BatchOperationName, entityType: string, remark?: string) {
    return new Promise((resolve, reject) => {
      let ids = this.selectedData.map(x => x["id"]);
      let text = "";
      switch (sth) {
        case "delete":
        case "submit":
          ids = this.selectedData.filter(x => x["auditStatus"] === AuditStatus.Default).map(x => x["id"]);
          text = "只能操作未上报状态的数据";
          break;
        case "audit":
        case "unsubmit":
          ids = this.selectedData.filter(x => x["auditStatus"] === AuditStatus.Submitted).map(x => x["id"]);
          text = "只能操作已上报状态的数据";
          break;
        case "unaudit":
          ids = this.selectedData.filter(x => x["auditStatus"] === AuditStatus.Audited).map(x => x["id"]);
          text = "只能操作已审核状态的数据";
          break;
        default:
          break;
      }
      if (ids.length !== this.selectedData.length) {
        return this.msgSrv.warning(text);
      }
      if (!ids.any()) {
        this.msgSrv.warning("至少选择一项");
        return;
      }

      let apiName = gql`
      mutation ${sth}${entityType}($ids: [String], $remark:String) {
        ${sth}${entityType}(ids: $ids, remark:$remark)
      }
      `;
      if (sth === "delete") {
        apiName = gql`
          mutation ${sth}${entityType}($ids: [String!]!) {
            ${sth}${entityType}(ids: $ids)
          }
        `;
      }

      let alertMessage = this.I18N.Common.action.get(sth);
      this.nzModalSrv.confirm({
        nzTitle: `确认${alertMessage}吗？`,
        nzOnOk: async () => {
          await this.apollo
            .mutate({
              mutation: apiName,
              variables: {
                remark,
                ids,
              },
            })
            .toPromise();
          resolve(true);
          this.msgSrv.success(this.I18N.Common.message.get(sth));
          this.refresh();
        },
      });
    });
  }
}
