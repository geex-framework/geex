import { Component, computed, signal } from "@angular/core";
import { FormControl, FormGroup } from "@angular/forms";
import { STChange } from "@delon/abc/st";
import { gql } from "apollo-angular";

import { GeexHint } from "../types";
import { RoutedComponent } from "./routed.component.base";

export type BatchOperationName = "delete" | "approve" | "submit" | "unApprove" | "unSubmit";

export class ListPageParams<T> {
  pi?: number;
  ps?: number;
  /** Host GraphQL sort input; kept loose to match module-specific SortInput types. */
  sort?: any;
}

@Component({ template: "", standalone: true })
export abstract class RoutedListComponent<
  TParams extends ListPageParams<TDto>,
  TDto extends GeexHint<{ id?: string }>,
> extends RoutedComponent<TParams> {
  data = signal<Array<GeexHint<TDto>>>([]);
  abstract columns?: any;
  total = signal(0);
  selectedData = signal<Array<GeexHint<TDto>>>([]);
  allSelected = computed(() => {
    return this.selectedData().length > 0 && this.data()?.length == this.selectedData().length;
  });

  onAllChecked(value: boolean): void {
    this.selectedData.set(value ? this.data() : []);
  }

  onItemChecked(data: TDto, checked: boolean): void {
    if (checked) {
      this.selectedData.update(selectedData => [...selectedData, data]);
    } else {
      this.selectedData.update(selectedData => selectedData.filter(x => x.id !== data.id));
    }
  }

  async tableChange(args: STChange) {
    if (args.type == "loaded") {
      return;
    }

    if (args.type == "checkbox") {
      this.onTableCheckbox(args);
      return;
    }

    if (args.type == "pi" || args.type == "ps") {
      this.onTablePage(args);
    }

    if (args.sort?.column?.index) {
      this.onTableSort(args);
    }
  }

  protected onTableCheckbox(args: STChange): void {
    this.selectedData.set(args.checkbox as any);
  }

  protected onTablePage(args: STChange): void {
    if (args.pi !== this.paramsForm.value.pi || args.ps !== this.paramsForm.value.ps) {
      this.paramsForm.patchValue({ pi: args.pi, ps: args.ps });
      this.refresh();
    }
  }

  protected onTableSort(args: STChange): void {
    const thisSortName = (args.sort!.column as any)["indexKey"];
    let sorts = (args.sort!.map as any)["sort"].split("-").map((x: string) => x.split("."));
    const thisSort = sorts.find((x: string[]) => x[0] == thisSortName);
    if (thisSort) {
      sorts = sorts.filter((x: string[]) => x !== thisSort);
    }
    sorts.push(thisSort);
    sorts = sorts.filter((x: string[] | undefined) => x != undefined && x[0] != "");
    const sortsForm = new FormGroup(Object.fromEntries(sorts.map((x: string[]) => [x[0], new FormControl(x[1])])));
    this.paramsForm.setControl("sort", sortsForm as any);
    this.refresh();
  }

  batchOperation(operation: BatchOperationName, entityType: string, remark?: string): Promise<boolean> {
    return new Promise((resolve) => {
      const selectedData = this.selectedData();
      const filtered = this.filterBatchIds(operation, selectedData);
      if (filtered.error) {
        this.msgSrv.warning(filtered.error);
        resolve(false);
        return;
      }
      const ids = filtered.ids;
      if (!ids.length) {
        this.msgSrv.warning("至少选择一项");
        resolve(false);
        return;
      }

      const apiName = this.buildBatchMutation(operation, entityType);
      this.confirmBatch(operation, apiName, ids, remark).then(resolve);
    });
  }

  protected filterBatchIds(
    operation: BatchOperationName,
    selectedData: Array<GeexHint<TDto>>,
  ): { ids: any[]; error?: string } {
    let ids = selectedData.map(x => x["id"]);
    if (selectedData[0]?.["approveStatus"] == undefined) {
      return { ids };
    }
    let text = "";
    switch (operation) {
      case "delete":
      case "submit":
        ids = selectedData.filter(x => x["approveStatus"] === "DEFAULT").map(x => x["id"]);
        text = "只能操作未上报状态的数据";
        break;
      case "approve":
      case "unSubmit":
        ids = selectedData.filter(x => x["approveStatus"] === "SUBMITTED").map(x => x["id"]);
        text = "只能操作已上报状态的数据";
        break;
      case "unApprove":
        ids = selectedData.filter(x => x["approveStatus"] === "APPROVED").map(x => x["id"]);
        text = "只能操作已审核状态的数据";
        break;
      default:
        break;
    }
    if (ids.length !== selectedData.length) {
      return { ids, error: text };
    }
    return { ids };
  }

  protected buildBatchMutation(operation: BatchOperationName, entityType: string): string {
    if (operation === "delete") {
      return `
          mutation ${operation}${entityType}($ids: [String!]!) {
            ${operation}${entityType}(ids: $ids)
          }
        `;
    }
    return `
      mutation ${operation}${entityType}($ids: [String], $remark:String) {
        ${operation}${entityType}(ids: $ids, remark:$remark)
      }
      `;
  }

  protected confirmBatch(
    operation: BatchOperationName,
    apiName: string,
    ids: any[],
    remark?: string,
  ): Promise<boolean> {
    return new Promise((resolve) => {
      const common = (this.I18N as { Common?: { action?: { get?(k: string): string }; message?: { get?(k: string): string } } }).Common;
      const alertMessage = common?.action?.get?.(operation) ?? operation;
      this.nzModalSrv.confirm({
        nzTitle: `确认${alertMessage}吗？`,
        nzOnOk: async () => {
          await (this.apollo.mutate({
            mutation: gql(apiName),
            variables: {
              remark,
              ids,
            },
          }) as any).firstValuePromise();
          this.msgSrv.success(common?.message?.get?.(operation) ?? "ok");
          this.refresh();
          resolve(true);
        },
        nzOnCancel: () => resolve(false),
      });
    });
  }
}
