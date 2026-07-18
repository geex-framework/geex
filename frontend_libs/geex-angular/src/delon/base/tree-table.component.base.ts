import { Component, inject } from "@angular/core";

import { GEEX_I18N } from "../tokens";

@Component({
  template: "",
  standalone: true,
})
export abstract class TreeTableComponentBase<ITreeNode = any> {
  mapOfExpandedData: { [key: string]: ITreeNode[] } = {};
  I18N = inject(GEEX_I18N, { optional: true }) as any;

  protected getNodeKey(node: ITreeNode): string {
    return (node as any)["key"];
  }

  protected getNodeChildren(node: ITreeNode): ITreeNode[] | undefined {
    return (node as any)["children"];
  }

  collapse(array: ITreeNode[], data: ITreeNode, $event: boolean): void {
    if (!$event) {
      const children = this.getNodeChildren(data);
      if (children) {
        children.forEach(d => {
          const target = array.find(a => this.getNodeKey(a) === this.getNodeKey(d))!;
          (target as any)["expand"] = false;
          this.collapse(array, target, false);
        });
      }
    }
  }

  convertTreeToList(root: ITreeNode): ITreeNode[] {
    const stack: ITreeNode[] = [];
    const array: ITreeNode[] = [];
    const hashMap: { [key: string]: boolean } = {};
    stack.push({ ...(root as any), level: 0, expand: false });

    while (stack.length !== 0) {
      const node = stack.pop()!;
      this.visitNode(node, hashMap, array);
      const children = this.getNodeChildren(node);
      if (children) {
        for (let i = children.length - 1; i >= 0; i--) {
          stack.push({
            ...(children[i] as any),
            level: ((node as any)["level"] ?? 0) + 1,
            expand: false,
            parent: node,
          });
        }
      }
    }

    return array;
  }

  visitNode(node: ITreeNode, hashMap: { [key: string]: boolean }, array: ITreeNode[]): void {
    const key = this.getNodeKey(node);
    if (!hashMap[key]) {
      hashMap[key] = true;
      array.push(node);
    }
  }
}
