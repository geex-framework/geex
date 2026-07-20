import { inject, Injectable, Injector } from "@angular/core";
import { Router } from "@angular/router";
import { OAuthErrorEvent, OAuthService } from "angular-oauth2-oidc";
import { NzModalService } from "ng-zorro-antd/modal";
import { filter, firstValueFrom, interval, map, takeUntil, timer } from "rxjs";

import { ACLService } from "@delon/acl";
import { Menu, MenuService, SettingsService } from "@delon/theme";
import { CookieService } from "@delon/util";

import { DebuggerBlockerService } from "../debugger-blocker.service";
import { Geex } from "../geex";
import { GEEX_AFTER_LOGIN_NAVIGATE, GEEX_LOGIN_PATH } from "../http/tokens";
import { GEEX_DEFAULT_MENUS, GEEX_MENU_CONTRIBUTIONS } from "../menu-contribution";
import { GEEX_I18N_SERVICE } from "../delon/tokens";
import { GEEX_LOCALIZATION_DATA_SETTING, GEEX_LOCALIZATION_LANGUAGE_SETTING } from "../i18n/tokens";
import { GEEX_APP_MENU_SETTING, GEEX_APP_NAME_SETTING } from "../settings-setting-names";
import { GEEX_SUPER_ADMIN_USER_ID } from "../tokens/identity.tokens";
import type { GeexStartupI18nAdapter } from "./types";
import { GEEX_EXCEPTION_500_PATH, GEEX_SESSION_TERMINATED_COPY, GEEX_STARTUP_OPTIONS } from "./tokens";

/**
 * Single bootstrap entry for app session.
 *
 * Linear flow:
 * 1. configure OAuth
 * 2. if OIDC callback code present → tryLogin (once)
 * 3. geex.init()
 * 4. bind Delon user / ACL / menus
 * 5. start session watch (once)
 *
 * Login pages must not call tryLogin/load for OIDC callbacks.
 * Password / WeChat token handoff uses initCodeFlow → IdP → this bootstrap again.
 */
@Injectable()
export class GeexStartupService {
  private readonly options = inject(GEEX_STARTUP_OPTIONS);
  private readonly injector = inject(Injector);
  private readonly geex = inject(Geex);
  private readonly oAuthService = inject(OAuthService);
  private readonly aclService = inject(ACLService);
  private readonly settingsService = inject(SettingsService);
  private readonly router = inject(Router);
  private readonly modalService = inject(NzModalService);
  private readonly menuService = inject(MenuService);
  private readonly loginPath = inject(GEEX_LOGIN_PATH);
  private readonly afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
  private readonly superAdminUserId = inject(GEEX_SUPER_ADMIN_USER_ID);
  private readonly exception500Url = inject(GEEX_EXCEPTION_500_PATH);
  private readonly sessionTerminatedCopy = inject(GEEX_SESSION_TERMINATED_COPY);
  private readonly defaultMenus = inject(GEEX_DEFAULT_MENUS);
  private readonly debuggerBlocker = inject(DebuggerBlockerService);

  private bootstrapPromise: Promise<void> | null = null;
  private bootstrapped = false;
  private sessionWatchStarted = false;

  /** APP_INITIALIZER entry. Safe to call concurrently; runs the bootstrap pipeline once. */
  async load(): Promise<void> {
    if (this.bootstrapped) {
      return;
    }
    if (this.bootstrapPromise) {
      return this.bootstrapPromise;
    }
    this.bootstrapPromise = this.bootstrap().finally(() => {
      this.bootstrapPromise = null;
    });
    return this.bootstrapPromise;
  }

  private async bootstrap(): Promise<void> {
    try {
      this.debuggerBlocker.init();
      this.oAuthService.configure(this.options.oauth.getConfig());
      await this.trySwitchTenant();
      await this.tryAutoOAuthLogin();
      await this.tryOidcCodeCallback();
      this.ensureSessionWatch();
      await this.geex.init();
      await this.bindUiSession();
      this.bootstrapped = true;
    } catch (error) {
      await this.router.navigateByUrl(this.exception500Url);
      console.error(error);
    }
  }

  private async tryOidcCodeCallback(): Promise<void> {
    const url = new URL(location.href);
    const code = url.searchParams.get("code");
    if (!code) {
      return;
    }
    const state = url.searchParams.get("state") ?? "";
    // WeChat QR callback uses the same ?code= param but is handled on the login page first.
    if (state === "WechatWeb" || state.startsWith("WechatWeb")) {
      return;
    }
    // Discovery aligns issuer/jwks before tryLogin (id_token validation). tokenEndpoint alone is not enough.
    try {
      await this.oAuthService.loadDiscoveryDocument();
    } catch (err) {
      console.error(err);
    }
    await this.ensureOAuthTokenEndpoint();
    await this.oAuthService.tryLogin();
  }

  /** Fill tokenEndpoint gaps after discovery (or when discovery is unreachable). */
  private async ensureOAuthTokenEndpoint(): Promise<void> {
    if (this.oAuthService.tokenEndpoint) {
      return;
    }
    const issuer = this.oAuthService.issuer?.replace(/\/?$/, "/");
    if (issuer) {
      this.oAuthService.tokenEndpoint = `${issuer}idsvr/token`;
      return;
    }
    throw new Error(
      "OAuth tokenEndpoint is not configured. Set AuthConfig.tokenEndpoint (e.g. {issuer}/idsvr/token) or make OIDC discovery reachable.",
    );
  }

  private async bindUiSession(): Promise<void> {
    if (!this.oAuthService.hasValidAccessToken()) {
      return;
    }
    const user = await this.resolveAuthUser();
    if (!user) {
      // Token without federateAuthenticate user is not a completed Geex login.
      console.error("bindUiSession: access token present but geex.authentication.user() missing after federateAuthenticate");
      this.oAuthService.logOut(true);
      return;
    }
    this.settingsService.setUser({
      avatar: user.avatarFile?.url,
      id: user.id,
      phoneNumber: user.phoneNumber,
      email: user.email,
      username: user.username,
      roleName: user.roleNames,
    });
    const adminId = this.superAdminUserId;
    if (user.id == adminId) {
      this.aclService.setFull(true);
    } else {
      this.aclService.setRole(user.permissions);
    }
    type SettingRow = { name?: string; value?: unknown };
    const settingsModule = this.geex["settings"] as { settings: () => SettingRow[] } | undefined;
    const settings = settingsModule?.settings?.() ?? [];
    if (!settings.length) {
      return;
    }
    const appName = settings.find(x => x?.name == GEEX_APP_NAME_SETTING)?.value;
    if (appName) {
      this.settingsService.setApp({ name: appName as string });
    }
    let menus = this.defaultMenus.map(menu => ({
      ...menu,
      children: menu.children ? [...menu.children] : menu.children,
    })) as Menu[];
    const settingMenus = settings.find(x => x?.name == GEEX_APP_MENU_SETTING)?.value;
    if (Array.isArray(settingMenus) && settingMenus.length) {
      menus = (settingMenus as Menu[]).map(menu => ({
        ...menu,
        children: menu.children ? [...menu.children] : menu.children,
      }));
    }
    const contributions = this.injector.get(GEEX_MENU_CONTRIBUTIONS, []);
    const contributedGroups: Menu[] = [];
    for (const contribution of contributions) {
      for (const item of (await contribution.resolve(user)) as Menu[]) {
        if (item.group === true) {
          contributedGroups.push(item);
          continue;
        }
        // Leaf items must stay under a group. Delon top-level `group !== false` renders
        // as a non-clickable title without icons; never promote bare leaves to top-level.
        const leaf: Menu = {
          ...item,
          group: false,
          children: Array.isArray(item.children) ? item.children : [],
        };
        const existing = leaf.link ? this.findMenuByLink(menus, leaf.link) : undefined;
        if (existing) {
          Object.assign(existing, leaf, { hide: leaf.hide ?? false, group: false });
          continue;
        }
        const systemConfigGroup = this.findSystemConfigGroup(menus);
        if (systemConfigGroup) {
          systemConfigGroup.children = [...(systemConfigGroup.children ?? []), leaf];
        } else {
          contributedGroups.push({
            group: true,
            hideInBreadcrumb: true,
            open: true,
            text: "系统及配置",
            i18n: "Common.menu.systemConfig",
            children: [leaf],
          });
        }
      }
    }
    this.menuService.add([...menus, ...contributedGroups]);
    this.menuService.resume();
    const i18n = this.resolveI18nAdapter();
    i18n?.merge(settings.find(x => x?.name == GEEX_LOCALIZATION_DATA_SETTING)?.value as object);
    const backendLang = settings.find(x => x?.name == GEEX_LOCALIZATION_LANGUAGE_SETTING)?.value as string | undefined;
    if (backendLang) {
      this.settingsService.setLayout("lang", backendLang);
      i18n?.use(backendLang);
    } else {
      const cachedLang = this.settingsService.layout.lang as string | undefined;
      if (cachedLang) {
        i18n?.use(cachedLang);
      }
    }
  }

  async tryAutoOAuthLogin(): Promise<void> {
    const url = new URL(location.href);
    const autoLogin = url.searchParams.get("_autoLogin");
    if (autoLogin) {
      url.searchParams.delete("_autoLogin");
      this.oAuthService.redirectUri = url.href;
      this.oAuthService.initCodeFlow();
      throw new Error("starting auto login");
    }
  }

  private findMenuByLink(menus: Menu[], link: string): Menu | undefined {
    for (const menu of menus) {
      if (menu.link === link) {
        return menu;
      }
      if (menu.children?.length) {
        const found = this.findMenuByLink(menu.children, link);
        if (found) {
          return found;
        }
      }
    }
    return undefined;
  }

  private findSystemConfigGroup(menus: Menu[]): Menu | undefined {
    return menus.find(
      m =>
        m.i18n === "Common.menu.systemConfig" ||
        m.i18n === "menu.systemConfig" ||
        m.text === "系统及配置" ||
        m.children?.some(
          c =>
            c.link === "/settings" ||
            c.link === "/tenant" ||
            c.link === "/blob-storage" ||
            c.link === "/mocking" ||
            c.i18n === "Common.menu.settings" ||
            c.i18n === "Common.menu.tenant" ||
            c.i18n === "Mocking.title",
        ),
    );
  }

  private resolveI18nAdapter(): GeexStartupI18nAdapter | null {
    const service = this.injector.get(GEEX_I18N_SERVICE, null);
    if (service && typeof (service as GeexStartupI18nAdapter).merge === "function" && typeof (service as GeexStartupI18nAdapter).use === "function") {
      return service as GeexStartupI18nAdapter;
    }
    return null;
  }

  /** Safe read: guardedSignal throws before auth.init finishes. */
  private readAuthUser() {
    try {
      return this.geex.authentication.user() ?? undefined;
    } catch {
      return undefined;
    }
  }

  /**
   * After OIDC, auth.init may have finished with a null user (token race) or still be settling.
   * Never throw EmptyError into bootstrap (that becomes the 500 page).
   */
  private async resolveAuthUser() {
    let user = this.readAuthUser();
    if (user) {
      return user;
    }
    const auth = this.geex.authentication as { reload?: () => Promise<void> };
    if (typeof auth.reload === "function") {
      await auth.reload();
      user = this.readAuthUser();
      if (user) {
        return user;
      }
    }
    return firstValueFrom(
      interval(100).pipe(
        filter(() => this.readAuthUser() != undefined),
        map(() => this.readAuthUser()),
        takeUntil(timer(5000)),
      ),
      { defaultValue: undefined },
    );
  }

  private async trySwitchTenant(): Promise<void> {
    const url = new URL(location.href);
    const targetTenantCode = url.searchParams.get("__tenant");
    url.searchParams.delete("__tenant");
    if (!targetTenantCode) {
      return;
    }
    const currentTenantCode = this.injector.get(CookieService).get("__tenant");
    if (targetTenantCode == currentTenantCode) {
      await this.router.navigateByUrl(url.pathname + url.search + url.hash);
      return;
    }
    this.geex.multiTenant.switchTenant(targetTenantCode);
    await this.router.navigateByUrl(url.pathname + url.search + url.hash);
  }

  private ensureSessionWatch(): void {
    if (this.sessionWatchStarted) {
      return;
    }
    this.sessionWatchStarted = true;
    this.oAuthService.setupAutomaticSilentRefresh();
    this.oAuthService["initSessionCheck"]();
    const loginUrl = this.loginPath;
    const modalCopy = this.sessionTerminatedCopy;
    this.oAuthService.events.subscribe(e => {
      if (e instanceof OAuthErrorEvent && (e.reason as { status?: number } | undefined)?.status == 401) {
        this.oAuthService.logOut(true);
      }
      if (e.type == "session_terminated") {
        console.error(e);
        this.modalService.info({
          nzTitle: modalCopy.title ?? "检测到账号切换, 请重新登入",
          nzOkText: modalCopy.okText ?? "确认",
          nzOnOk: async () => {
            this.settingsService.setUser({});
            this.aclService.set({});
            await this.router.navigateByUrl(loginUrl).then(() => {
              this.afterLoginNavigate();
            });
          },
          nzClosable: false,
        });
      }
    });
  }
}
