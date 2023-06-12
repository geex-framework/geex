import { Component, Injector, OnInit } from "@angular/core";
import { I18N } from "@core";
@Component({
  template: "",
})
export abstract class TreeTableComponentBase<ITreeNode = any> {
  mapOfExpandedData: { [key: string]: ITreeNode[] } = {};
  I18N = I18N;
  constructor(injector: Injector) {}
  collapse(array: ITreeNode[], data: ITreeNode, $event: boolean): void {
    if (!$event) {
      if (data["children"]) {
        data["children"].forEach(d => {
          const target = array.find(a => a["key"] === d.key)!;
          target["expand"] = false;
          this.collapse(array, target, false);
        });
      } else {
        return;
      }
    }
  }

  convertTreeToList(root: ITreeNode): ITreeNode[] {
    const stack: ITreeNode[] = [];
    const array: ITreeNode[] = [];
    const hashMap = {};
    stack.push({ ...root, level: 0, expand: false });

    while (stack.length !== 0) {
      const node = stack.pop()!;
      this.visitNode(node, hashMap, array);
      if (node["children"]) {
        for (let i = node["children"].length - 1; i >= 0; i--) {
          stack.push({ ...node["children"][i], level: node["level"]! + 1, expand: false, parent: node });
        }
      }
    }

    return array;
  }

  visitNode(node: ITreeNode, hashMap: { [key: string]: boolean }, array: ITreeNode[]): void {
    if (!hashMap[node["key"]]) {
      hashMap[node["key"]] = true;
      array.push(node);
    }
  }
}
