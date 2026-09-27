import { Injectable, InjectionToken, Injector, inject, makeEnvironmentProviders } from "@angular/core";
import { geex } from "@geexcode/geex-angular";

export interface MessageActionMessage { id: string; meta?: unknown; }
export interface MessageAction {
  readonly key: string;
  label(injector: Injector): string;
  matches(message: MessageActionMessage): boolean;
  allowed(injector: Injector, message: MessageActionMessage): boolean;
  execute(injector: Injector, message: MessageActionMessage): Promise<boolean>;
}
export const GEEX_MESSAGE_ACTIONS = new InjectionToken<readonly (readonly MessageAction[])[]>("GEEX_MESSAGE_ACTIONS");
export function provideGeexMessageActions(actions: readonly MessageAction[]) {
  return makeEnvironmentProviders([{ provide: GEEX_MESSAGE_ACTIONS, multi: true, useValue: actions }]);
}
@Injectable({ providedIn: "root" })
export class GeexMessageActions {
  private readonly injector = inject(Injector);
  readonly registered = (inject(GEEX_MESSAGE_ACTIONS, { optional: true }) ?? []).flat();
  private readonly executing = new Set<string>();
  constructor() {
    if (new Set(this.registered.map(action => action.key)).size !== this.registered.length)
      throw new Error("Duplicate message action key.");
  }
  label(action: MessageAction): string { return action.label(this.injector); }
  available(action: MessageAction, message: MessageActionMessage): boolean {
    return action.matches(message) && action.allowed(this.injector, message);
  }
  async execute(key: string, message: MessageActionMessage, userId: string | undefined): Promise<boolean> {
    const action = this.registered.find(item => item.key === key);
    if (!userId || !action || !this.available(action, message) || this.executing.has(message.id)) return false;
    this.executing.add(message.id);
    try {
      if (!await action.execute(this.injector, message)) return false;
      return await geex.messaging.markMessagesRead([message.id], userId);
    } finally { this.executing.delete(message.id); }
  }
}
