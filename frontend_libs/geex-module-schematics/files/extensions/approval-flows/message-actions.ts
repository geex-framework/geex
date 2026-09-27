import type zhCN from "./i18n/zh-CN";
import { Router } from "@angular/router";
import { geex, GEEX_I18N } from "@geexcode/geex-angular";
import type { MessageAction } from "@geexcode/geex-extensions-messaging";

export function approvalFlowIdFromMeta(meta: unknown): string | null {
  if (!meta || typeof meta !== "object") return null;
  const record = meta as Record<string, unknown>;
  const id = record["ApprovalFlowId"] ?? record["approvalFlowId"];
  return typeof id === "string" && id.trim().length > 0 ? id : null;
}
export const messageActions: readonly MessageAction[] = [{
  key: "approval-flows.handle",
  label: injector => (injector.get(GEEX_I18N) as { ApprovalFlows: typeof zhCN }).ApprovalFlows.handleMessage,
  matches: message => !!approvalFlowIdFromMeta(message.meta),
  allowed: () => !!geex.authentication.user()?.id,
  execute: (injector, message) => injector.get(Router).navigate(["/approval-flows/inbox", approvalFlowIdFromMeta(message.meta)]),
}];
