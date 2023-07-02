import { SFSchema } from "@delon/form";

export type FormProps<T> = {
  [key in
    | keyof Partial<T>
    | (string & {
        _?: never;
      })]?: SFSchema;
};

export enum OrgPickerLevel {
  All = "All",
  Area = "Area",
  Project = "Project",
}
/** 视图周期 年/季/月 */
export enum ViewInterval {
  Month = "Month",
  Season = "Season",
  Year = "Year",
}
export interface OrganizationUnitDto {
  id?: string;
  parentId?: string | undefined;
  code?: string | undefined;
  customCode?: string | undefined;
  discriminator?: string | undefined;
  displayName?: string | undefined;
}
export interface CompanyDto {
  id: string;
  code: string | undefined;
  name: string | undefined;
  simpName?: string | undefined;
  areaOrganizationUnitId?: string;
}
export interface MechanismDto {
  id: string;
  name: string;
}
export interface CityDto {
  id: string;
  name: string;
}
export interface LoanTypeDto {
  code: string;
  name: string;
  parentCode: string;
  isGroup: boolean;
  isEnabled: boolean;
  order: number;
}
export interface TreeNodeBaseInterface {
  key: string;
  name: string;
  age?: number;
  level?: number;
  expand?: boolean;
  address?: string;
  children?: TreeNodeInterface[];
  parent?: TreeNodeInterface;
}
export interface TreeNodeInterface<T = { [key: string]: any }> {
  id: number | string;
  children: Array<TreeNodeInterface<T>>;
  parent?: TreeNodeInterface<T>;
  expand: boolean;
  level: number;
  parentId?: number | string;
  data: T;
}

export function buildTreeNode<T>(nodes: Array<TreeNodeInterface<T>>, id: string | number, shrink = false): TreeNodeInterface<T> {
  if (id === undefined || id === "" || id === 0) {
  }
  let node = nodes.find(x => x.id == id);
  if (shrink) {
    nodes = nodes.filter(x => x.id != id);
  }
  let children = nodes.filter(y => y.parentId == id);
  node.children = children;
  children.map(
    x =>
      ({
        data: x.data,
        id: x.id,
        parentId: x.parentId,
        children: nodes.filter(y => y.parentId == x.id).map(y => buildTreeNode(nodes, x.id, shrink)),
      } as TreeNodeInterface<T>),
  );
  return node;
}
