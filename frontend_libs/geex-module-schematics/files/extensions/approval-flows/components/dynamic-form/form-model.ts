import { ApprovalFormFieldType } from "@/gql";

export interface ApprovalUiField {
  key: string;
  name: string;
  type: ApprovalFormFieldType;
  required: boolean;
  options: string[];
  allowEdit?: boolean;
}

export interface ApprovalUiValue {
  key: string;
  values: string[];
}

export const approvalFieldTypes = [
  { value: ApprovalFormFieldType.Text, label: "文本" },
  { value: ApprovalFormFieldType.MultilineText, label: "多行文本" },
  { value: ApprovalFormFieldType.Select, label: "单选" },
  { value: ApprovalFormFieldType.MultiSelect, label: "多选" },
  { value: ApprovalFormFieldType.Date, label: "日期" },
  { value: ApprovalFormFieldType.User, label: "人员" },
  { value: ApprovalFormFieldType.File, label: "文件" },
];

export function cleanFields(fields: ApprovalUiField[], flow: boolean): ApprovalUiField[] {
  return fields.map(field => ({
    key: field.key,
    name: field.name,
    type: field.type,
    required: field.required,
    options: [...field.options],
    ...(flow ? { allowEdit: !!field.allowEdit } : {}),
  }));
}

export function cleanValues(values: ApprovalUiValue[]): ApprovalUiValue[] {
  return values.map(value => ({ key: value.key, values: [...value.values] }));
}
