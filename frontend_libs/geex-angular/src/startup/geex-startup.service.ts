import { inject, Injectable, Injector, runInInjectionContext } from "@angular/core";
import { Router } from "@angular/router";
import { OAuthErrorEvent, OAuthService } from "angular-oauth2-oidc";
import { NzModalService } from "ng-zorro-antd/modal";
import { filter, firstValueFrom, interval, map, takeUntil, timer } from "rxjs";

import { ACLService } from "@delon/acl";
import { Menu, MenuService, SettingsService } from "@delon/theme";
import { CookieService } from "@delon/util";

import { Geex } from "../geex";
import { GEEX_AFTER_LOGIN_NAVIGATE, GEEX_LOGIN_PATH } from "../http/tokens";
import { GEEX_MENU_CONTRIBUTIONS } from "../menu-contribution";
import { GEEX_I18N_SERVICE } from "../delon/tokens";
import { GEEX_SUPER_ADMIN_USER_ID } from "../tokens/identity.tokens";
import type { GeexStartupI18nAdapter } from "./types";
import { GEEX_STARTUP_OPTIONS } from "./tokens";

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
    const exception500Url = this.options.exception500Url ?? "/exception/500";
    try {
      runInInjectionContext(this.injector, () => this.options.onDebuggerInit?.());
      this.oAuthService.configure(this.options.getOAuthConfig());
      await this.trySwitchTenant();
      await this.tryAutoOAuthLogin();
      await this.tryOidcCodeCallback();
      this.ensureSessionWatch();
      await this.geex.init();
      await this.bindUiSession();
      this.bootstrapped = true;
    } catch (error) {
      await this.router.navigateByUrl(exception500Url);
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
    await this.oAuthService.tryLogin();
  }

  private async bindUiSession(): Promise<void> {
    if (!this.oAuthService.hasValidAccessToken()) {
      return;
    }
    const user = await firstValueFrom(
      interval(100).pipe(
        filter(() => this.geex.auth.user() != undefined),
        map(() => this.geex.auth.user()!),
        takeUntil(timer(1000)),
      ),
    );
    this.settingsService.setUser({
      avatar: user.avatarFile?.url,
      id: user.id,
      phoneNumber: user.phoneNumber,
      email: user.email,
      username: user.username,
      roleName: user.roleNames,
    });
    const adminId = this.options.superAdminUserId ?? this.superAdminUserId;
    if (user.id == adminId) {
      this.aclService.setFull(true);
    } else {
      this.aclService.setRole(user.permissions);
    }
    const settings = this.geex.settings.settings();
    if (!settings.any()) {
      return;
    }
    const keys = this.options.settingKeys;
    const appName = settings.firstOrDefault(x => x?.name == keys.appName)?.value;
    if (appName) {
      this.settingsService.setApp({ name: appName });
    }
    let menus = this.options.defaultMenus;
    const settingMenus = settings.firstOrDefault(x => x?.name == keys.appMenu)?.value;
    if (settingMenus?.length) {
      menus = settingMenus as Menu[];
    }
    const contributions = this.injector.get(GEEX_MENU_CONTRIBUTIONS, []);
    const contributed: Menu[] = [];
    for (const contribution of contributions) {
      contributed.push(...((await contribution.resolve(user)) as Menu[]));
    }
    this.menuService.add([...menus, ...contributed]);
    this.menuService.resume();
    const i18n = this.resolveI18nAdapter();
    i18n?.merge(settings.first(x => x?.name == keys.localizationData)?.value);
    i18n?.use(settings.first(x => x?.name == keys.localizationLanguage)?.value);
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

  private resolveI18nAdapter(): GeexStartupI18nAdapter | null {
    if (this.options.i18n) {
      return this.options.i18n;
    }
    const service = this.injector.get(GEEX_I18N_SERVICE, null);
    if (service && typeof (service as GeexStartupI18nAdapter).merge === "function" && typeof (service as GeexStartupI18nAdapter).use === "function") {
      return service as GeexStartupI18nAdapter;
    }
    return null;
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
    this.geex.tenant.switchTenant(targetTenantCode);
    await this.router.navigateByUrl(url.pathname + url.search + url.hash);
  }

  private ensureSessionWatch(): void {
    if (this.sessionWatchStarted) {
      return;
    }
    this.sessionWatchStarted = true;
    this.oAuthService.setupAutomaticSilentRefresh();
    this.oAuthService["initSessionCheck"]();
    const loginUrl = this.options.loginUrl ?? this.loginPath;
    const modalCopy = this.options.modalCopy ?? {};
    this.oAuthService.events.subscribe(e => {
      if (e instanceof OAuthErrorEvent && (e.reason as { status?: number } | undefined)?.status == 401) {
        this.oAuthService.logOut(true);
      }
      if (e.type == "session_terminated") {
        console.error(e);
        this.modalService.info({
          nzTitle: modalCopy.sessionTerminatedTitle ?? "检测到账号切换, 请重新登入",
          nzOkText: modalCopy.sessionTerminatedOkText ?? "确认",
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
