import { Component, EventEmitter, inject, Input, OnInit, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ArrayService } from "@delon/util";
import { NzFormatEmitEvent, NzTreeNode, NzTreeNodeOptions } from "ng-zorro-antd/tree";
import { NzTreeModule } from "ng-zorro-antd/tree";
import { NzInputModule } from "ng-zorro-antd/input";
import { NzIconModule } from "ng-zorro-antd/icon";

import { GEEX_APP_PERMISSION, GEEX_I18N } from "@geexcode/geex-angular";
import { GEEX_PERMISSION_FILTER } from "@geexcode/geex-extensions-identity";

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
  standalone: true,
  imports: [FormsModule, NzTreeModule, NzInputModule, NzIconModule],
})
export class PermissionsComponent implements OnInit {
  @Input() permissions!: string[];
  @Output() readonly permissionsChange = new EventEmitter<string[]>();

  private readonly i18n = inject(GEEX_I18N);
  private readonly appPermission = inject(GEEX_APP_PERMISSION);
  private readonly permissionFilter = inject(GEEX_PERMISSION_FILTER);

  defaultExpandedKeys = ["all"];
  searchValue = "";
  nodes: NzTreeNodeOptions[] = [];

  constructor(private arrSrv: ArrayService) {}

  async ngOnInit(): Promise<void> {
    const allPermissions = this.permissionFilter(Object.values(this.appPermission));
    const parentNodeKeys: string[] = [];
    const parentNodes: NzTreeNodeOptions[] = [];
    const childNodes: NzTreeNodeOptions[] = [];
    allPermissions.forEach(x => {
      const moduleName = x.split("_")[0];
      childNodes.push({
        title: this.i18n.permissions.get(x),
        key: x,
        parentName: moduleName,
      });
      if (parentNodeKeys.includes(moduleName)) {
        return;
      }
      parentNodeKeys.push(moduleName);
      parentNodes.push({
        title: this.i18n.permissions.get(moduleName),
        key: moduleName,
        isLeaf: false,
        parentName: "all",
      });
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

  nzClick(_event: NzFormatEmitEvent): void {}

  nzCheck(_event: NzFormatEmitEvent): void {
    const checkedKeys: string[] = this.arrSrv.getKeysByTreeNode(this.nodes as NzTreeNode[]);
    const values = checkedKeys.filter(x => x.includes("_"));
    this.permissionsChange.emit(values);
  }
}
