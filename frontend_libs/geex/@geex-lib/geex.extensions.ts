import { signal, WritableSignal } from "@angular/core";
import { ITenant, Maybe, OrgBriefFragment, Setting, SettingBriefFragment, UserDetailFragment } from "../graphql/.generated/type";
import { debounceTime, fromEvent, switchMap } from "rxjs";
import { RoutedComponent } from "../components";
import { Observable } from "rxjs/internal/Observable";

declare global {
  const geex: typeof window.geex;
  interface Window {
    geex: {
      tenant: WritableSignal<Maybe<ITenant>>;
      auth: {
        user: WritableSignal<Maybe<UserDetailFragment>>;
      };
      identity: {
        orgs: WritableSignal<OrgBriefFragment[]>;
        userOwnedOrgs: WritableSignal<OrgBriefFragment[]>;
      };
      settings: WritableSignal<Array<Pick<Setting, "name" | "value">>>;
      fullScreen: WritableSignal<boolean>;
      isMobile$: Observable<boolean>;
      activeRoutedComponent?: RoutedComponent<any>;
    };
  }
}

Object.defineProperty(window, "geex", {
  value: {
    settings: signal<SettingBriefFragment[]>([]),
    fullScreen: signal<boolean>(false),
    isMobile$: fromEvent(window, "resize").pipe(
      // 防抖动，避免频繁触发（例如每200ms触发一次）
      debounceTime(200),
      // 提取窗口的宽度和高度
      switchMap(async () => window.innerHeight / window.innerWidth >= 1.5),
    ),
    identity: {
      orgs: signal<OrgBriefFragment[]>([]),
      userOwnedOrgs: signal<OrgBriefFragment[]>([]),
    },
    auth: {
      user: signal<Maybe<UserDetailFragment>>(undefined),
    },
    tenant: signal<Maybe<ITenant>>(
      localStorage.getItem("tenant") ? JSON.parse(localStorage.getItem("tenant")) : { code: "", name: "host" },
    ),
    activeRoutedComponent: undefined,
  } as typeof window.geex,
  writable: false,
});
