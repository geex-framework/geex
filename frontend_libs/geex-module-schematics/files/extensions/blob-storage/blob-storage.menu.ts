import type { Menu } from "@delon/theme";

// ACL: schema has no BlobStorage_* permission enum yet.
export const menuContribution: Menu[] = [
  { text: "文件存储", i18n: "BlobStorage.title", link: "/blob-storage", icon: "anticon-cloud-upload", children: [] },
];
