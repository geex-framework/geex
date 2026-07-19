import { Component, Inject, Injector, OnDestroy, OnInit, signal, Signal, ViewEncapsulation } from "@angular/core";
import { AbstractControl, FormBuilder, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";

import { OAuthErrorEvent, OAuthService } from "angular-oauth2-oidc";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzTabChangeEvent } from "ng-zorro-antd/tabs";

import { geex } from "@geexcode/geex-angular";
import { ACLService } from "@delon/acl";
import { _HttpClient, ModalHelper, SettingsService } from "@delon/theme";
import { CookieService } from "@delon/util";
import { environment } from "@/environment";
import { authenticate } from "../graphql/operations.gql";
import type { Tenant } from "@geexcode/geex-extensions-identity";
import { SharedModule } from "@/shared/shared.module";

import { TenantSwitcherComponent } from "@/modules/tenant/components/tenant-switcher/tenant-switcher.component";
import SparkMD5 from "spark-md5";

@Component({
  selector: "auth-login",
  templateUrl: "./login.page.html",
  styleUrls: ["./login.page.scss"],
  standalone: true,
  imports: [SharedModule],
  encapsulation: ViewEncapsulation.None,
})
export class UserLoginComponent implements OnInit, OnDestroy {
  geex = geex;
  submitting = signal(false);
  oauthLoading = signal(false);
  verificationImgUrl = "";
  isMobile = geex.ui.isMobile;
  wechatEnabled = !!environment.auth.wechatWeb?.appId;

  constructor(
    fb: FormBuilder,
    private injector: Injector,
    private router: Router,
    private route: ActivatedRoute,
    private settingsService: SettingsService,
    private aclService: ACLService,
    private modalHelper: ModalHelper,
    public http: _HttpClient,
    public msg: NzMessageService,
    public apollo: Apollo,
    private cookie: CookieService,
    private oauthService: OAuthService,
  ) {
    this.form = fb.group({
      userName: ["superAdmin", [Validators.required]],
      password: ["superAdmin", [Validators.required]],
      // mobile: [null, [Validators.required, Validators.pattern(/^1\d{10}$/)]],
      // captcha: [null, [Validators.required]],
      remember: [true],
    });
    this.tenant$ = geex.tenant.current;
  }

  // #region fields
  tenant$: Signal<Tenant>;
  get userName(): AbstractControl {
    return this.form.controls.userName;
  }
  get password(): AbstractControl {
    return this.form.controls.password;
  }
  get mobile(): AbstractControl {
    return this.form.controls.mobile;
  }
  get captcha(): AbstractControl {
    return this.form.controls.captcha;
  }
  form: FormGroup;
  error = "";
  type = 0;

  // #region get captcha

  count = 0;
  interval$: any;

  // #endregion

  switch({ index }: NzTabChangeEvent): void {
    this.type = index!;
    if (this.wechatEnabled && this.type === 1) {
      void this.renderWechatQr();
    }
  }

  async ngOnInit(): Promise<void> {
    try {
      const queryParamMap = this.route.snapshot.queryParamMap;
      const state = queryParamMap.get("state") ?? "";
      const isWechatCallback = state === "WechatWeb" || state.startsWith("WechatWeb");

      if (queryParamMap.has("code") || queryParamMap.has("error")) {
        this.oauthLoading.set(true);
      }

      if (queryParamMap.has("code") && isWechatCallback) {
        try {
          const code = queryParamMap.get("code")!;
          const result = await geex.wechatAuth.resolveLogin({
            loginProvider: "WechatWeb",
            code,
          });
          if (result.isLinked && result.session?.token) {
            geex.wechatAuth.establishSession({
              token: result.session.token,
              returnUrl: this.route.snapshot.queryParamMap.get("redirect_uri") ?? "/",
            });
            return;
          }

          await this.router.navigate(["/auth/user-login-link"], {
            queryParams: {
              userLoginLinkToken: result.userLoginLinkToken,
              displayName: result.displayName,
            },
          });
          return;
        } finally {
          this.oauthLoading.set(false);
        }
      }

      if (queryParamMap.has("code")) {
        try {
          if (!this.oauthService.hasValidAccessToken()) {
            this.msg.error("登录会话未建立, 请重新登录");
            return;
          }
          const oauthState = this.oauthService.state;
          if (oauthState) {
            const target = decodeURIComponent(oauthState);
            if (target.startsWith("http")) {
              location.href = target;
            } else {
              await this.router.navigate([target]);
            }
            return;
          }
          await this.router.navigate(["/"]);
        } finally {
          this.oauthLoading.set(false);
        }
      }

      if (queryParamMap.has("error")) {
        const error = queryParamMap.get("error");
        if (error) {
          const description = decodeURIComponent(queryParamMap.get("error_description") ?? error);
          this.msg.error(description);
          await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
          });
        }
        this.oauthLoading.set(false);
      }
    } catch (err) {
      console.error("OAuth 回调处理失败", err);
      this.oauthLoading.set(false);

      if (err instanceof OAuthErrorEvent) {
        const error = err.params["error"];
        if (error) {
          const description = decodeURIComponent(err.params["error_description"] ?? error);
          this.msg.error(description);
          await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
          });
        }
      } else {
        this.msg.error((err as any)?.message ?? "登录失败");
      }
    }
  }

  async renderWechatQr(): Promise<void> {
    try {
      await geex.wechatAuth.renderQr({
        webAppId: environment.auth.wechatWeb!.appId,
        redirectUri: environment.auth.wechatWeb!.redirectUri,
        containerId: "wechat_login_container",
        state: "WechatWeb",
      });
      this.normalizeWechatQrSvg();
    } catch (e) {
      console.error(e);
      this.msg.error("微信登录组件加载失败");
    }
  }

  private normalizeWechatQrSvg(): void {
    const svg = document.querySelector<SVGSVGElement>("#wechat_login_container > svg");
    if (!svg) {
      return;
    }
    const width = Number.parseFloat(svg.getAttribute("width") ?? "");
    const height = Number.parseFloat(svg.getAttribute("height") ?? "");
    if (!svg.getAttribute("viewBox") && width > 0 && height > 0) {
      svg.setAttribute("viewBox", `0 0 ${width} ${height}`);
    }
    svg.removeAttribute("width");
    svg.removeAttribute("height");
    svg.setAttribute("preserveAspectRatio", "xMidYMid meet");
  }

  getCaptcha(): void {
    if (this.mobile.invalid) {
      this.mobile.markAsDirty({ onlySelf: true });
      this.mobile.updateValueAndValidity({ onlySelf: true });
      return;
    }
    this.count = 59;
    this.interval$ = setInterval(() => {
      this.count -= 1;
      if (this.count <= 0) {
        clearInterval(this.interval$);
      }
    }, 1000);
  }

  // #endregion

  async submit() {
    this.submitting.set(true);
    this.error = "";
    try {
      const res = await this.apollo
        .mutate({
          mutation: authenticate,
          variables: {
            request: {
              userIdentifier: this.userName.value.trim(),
              password: SparkMD5.hash(this.password.value.trim()),
              // tenantCode,
            },
          },
        })
        .firstValuePromise();
      const authResult = res.data.authenticate;
      if (!authResult.token) {
        this.error = res.error?.message;
        return;
      } else {
        const redirect_uri = this.route.snapshot.queryParamMap.get("redirect_uri") ?? "/";
        // 设置用户Token信息,过期时间
        this.oauthService.initCodeFlow(redirect_uri, { access_token: authResult.token });
      }
    } catch (error) {
      this.error = error;
    } finally {
      setTimeout(() => {
        this.submitting.set(false);
      }, 2000);
    }
  }

  ngOnDestroy(): void {
    if (this.interval$) {
      clearInterval(this.interval$);
    }
  }

  // #region 验证码功能
  onKey(e: KeyboardEvent): any {
    // if (e.key === "Tab") {
    //   this.initImg();
    // }
  }
  initImg(): void {
    // const userName = this.loginService.authenticateModel.userNameOrEmailAddress;
    // if (!userName || userName === "" || this.verificationImgUrl !== "") {
    //   return;
    // }
    // this.clearimg();
  }

  clearimg(): void {
    // const userName = this.loginService.authenticateModel.userNameOrEmailAddress;
    // if (!userName || userName === '') {
    //   // 未输入账号
    //   return;
    // }
    // let tid: any = this.appSession.tenantId;
    // if (!tid) {
    //   tid = '';
    // }
    // const timestamp = new Date().getTime();
    // this.verificationImgUrl =
    //   environment.remoteServiceBaseUrl +
    //   '/api/TokenAuth/GenerateVerification' +
    //   '?name=' +
    //   userName +
    //   '&tid=' +
    //   tid +
    //   '&t=' +
    //   timestamp;
  }
  async showTenantSwitcher() {
    const modal = this.modalHelper.create(TenantSwitcherComponent);
    await modal.firstValuePromise();
  }
}
