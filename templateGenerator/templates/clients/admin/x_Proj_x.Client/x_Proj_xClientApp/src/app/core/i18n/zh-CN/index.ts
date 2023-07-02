import { LangObject } from "../types";
import AppPermission from "./appPermission";
import common from "./common";
import setting from "./setting";

import { AuditStatus } from "@/app/shared/graphql/.generated/type";
export default {
  Settings: setting as LangObject<typeof setting>,
  shortcut: "快捷方式",
  AuditStatus: {
    Default: "待上报",
    Audited: "已审批",
    Submitted: "已上报",
    DEFAULT: "待上报",
    AUDITED: "已审批",
    SUBMITTED: "已上报",
  } as LangObject<typeof AuditStatus>,
  Acl: AppPermission as LangObject<typeof AppPermission>,
  Common: common as LangObject<typeof common>,
};
