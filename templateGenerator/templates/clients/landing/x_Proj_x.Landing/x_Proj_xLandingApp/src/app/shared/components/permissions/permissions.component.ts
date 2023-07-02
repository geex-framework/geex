import { Component, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from "@angular/core";
import { ArrayService } from "@delon/util";
import { Select } from "@ngxs/store";
import { NzFormatEmitEvent, NzTreeNode, NzTreeNodeOptions } from "ng-zorro-antd/tree";
import { Observable } from "rxjs";
import { take } from "rxjs/operators";

import { I18N, I18NService } from "../../../core";
import { AppPermission, ITenant } from "../../graphql/.generated/type";
import { TenantState } from "../../states/tenant.state";

@Component({
  selector: "app-permissions",
  templateUrl: "./permissions.component.html",
  styles: [
    `
      nz-input-group {
        margin-bottom: 12px;
        width: 100%;
      }
    `,
  ],
})
export class PermissionsComponent implements OnInit {
  @Input() permissions: string[];
  @Output() readonly permissionsChange = new EventEmitter<string[]>();
  I18N = I18N;
  defaultExpandedKeys = ["all"];
  searchValue = "";
  @Select(TenantState)
  tenant$: Observable<ITenant>;
  nodes: NzTreeNodeOptions[] = [];
  constructor(private arrSrv: ArrayService, private i18n: I18NService) {}
  async ngOnInit(): Promise<void> {
    let allPermissions = Object.values(AppPermission);
    let currentTenant = await this.tenant$.pipe(take(1)).toPromise();
    if (currentTenant?.code != undefined) {
      allPermissions = allPermissions.filter(x => !x.toString().startsWith("multiTenant_") && !x.toString().startsWith("settings_"));
    }
    let parentNodeKeys: string[] = [];
    let parentNodes = [];
    let childNodes = [];
    allPermissions.forEach(x => {
      let moduleName = x.split("_")[0];
      childNodes.push({
        title: this.I18N.Acl.get(x),
        key: x,
        parentName: moduleName,
      });
      if (parentNodeKeys.includes(moduleName)) {
        return;
      } else {
        parentNodeKeys.push(moduleName);
        parentNodes.push({
          title: this.I18N.Acl.get(moduleName),
          key: moduleName,
          isLeaf: false,
          parentName: "all",
        });
      }
    });
    this.nodes = this.arrSrv.arrToTreeNode(
      [
        {
          title: "全部",
          key: "all",
          children: [],
        },
        ...parentNodes,
        ...childNodes,
      ],
      {
        parentIdMapName: "parentName",
        idMapName: "key",
      },
    );
  }
  nzClick(event: NzFormatEmitEvent): void {
    //console.log(event);
  }

  nzCheck(event: NzFormatEmitEvent): void {
    let checkedKeys: string[] = this.arrSrv.getKeysByTreeNode(this.nodes as NzTreeNode[]);
    let values = checkedKeys.filter(x => x.includes("_"));
    this.permissionsChange.emit(values);
  }
}
