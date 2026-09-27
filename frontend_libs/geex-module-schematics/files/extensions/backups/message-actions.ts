import type zhCN from "./i18n/zh-CN";
import { Router } from "@angular/router";
import { ACLService } from "@delon/acl";
import { GEEX_I18N } from "@geexcode/geex-angular";
import type { MessageAction } from "@geexcode/geex-extensions-messaging";

export const messageActions: readonly MessageAction[] = [{
  key: "backups.view",
  label: injector => (injector.get(GEEX_I18N) as { Backups: typeof zhCN }).Backups.view,
  matches: message => {
    const meta = message.meta as { actions?: { key?: string }[] } | null;
    return Array.isArray(meta?.actions) && meta.actions.some(action => action?.key === "backups.view");
  },
  allowed: injector => injector.get(ACLService).can("Backups_query_backups"),
  execute: injector => injector.get(Router).navigate(["/backups"], {
    onSameUrlNavigation: "reload", state: { backupsReset: crypto.randomUUID() },
  }),
}];
