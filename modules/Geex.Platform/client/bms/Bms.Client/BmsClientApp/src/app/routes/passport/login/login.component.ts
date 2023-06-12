import { Component, Inject, Injector, OnDestroy, Optional } from "@angular/core";
import { AbstractControl, FormBuilder, FormGroup, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { StartupService } from "@core";
import { ReuseTabService } from "@delon/abc/reuse-tab";
import { ACLService } from "@delon/acl";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel, SocialOpenType, SocialService, TokenService } from "@delon/auth";
import { ModalHelper, SettingsService, TitleService, _HttpClient } from "@delon/theme";
import { CookieService, deepCopy } from "@delon/util";
import { environment } from "@env/environment";
import { Select, Store } from "@ngxs/store";
import { OAuthService } from "angular-oauth2-oidc";
import { Apollo, gql } from "apollo-angular";
import { HttpLink } from "apollo-angular/http";
import * as json5 from "json5";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzTabChangeEvent } from "ng-zorro-antd/tabs";
import { Observable } from "rxjs";

import { AuthenticateGql, CreateBlobObjectGql, ITenant, LoginProviderEnum } from "../../../shared/graphql/.generated/type";
import { TenantState } from "../../../shared/states/tenant.state";
import { TenantSwitcherComponent } from "../../saas/components//tenant-switcher/tenant-switcher.component";
@Component({
  selector: "passport-login",
  templateUrl: "./login.component.html",
  styleUrls: ["./login.component.scss"],
  providers: [SocialService],
})
export class UserLoginComponent implements OnDestroy {
  activeLoginProviders: [`${LoginProviderEnum}`] = ["Geex"];
  submitting = false;
  verificationImgUrl = "";
  isMobile: boolean = false;
  constructor(
    fb: FormBuilder,
    private injector: Injector,
    private router: Router,
    private settingsService: SettingsService,
    private socialService: SocialService,
    @Inject(DA_SERVICE_TOKEN) private tokenService: ITokenService,
    private startupSrv: StartupService,
    private aclService: ACLService,
    private modalHelper: ModalHelper,
    public http: _HttpClient,
    public msg: NzMessageService,
    public apollo: Apollo,
    private store: Store,
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
  }

  // #region fields
  @Select(TenantState)
  tenant$: Observable<ITenant>;
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
  }

  ngOnInit(): void {
    this.isMobile = window.isMobile;
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
    this.submitting = true;
    this.error = "";
    // if (this.type === 0) {
    //   this.userName.markAsDirty();
    //   this.userName.updateValueAndValidity();
    //   this.password.markAsDirty();
    //   this.password.updateValueAndValidity();
    //   if (this.userName.invalid || this.password.invalid) {
    //     return;
    //   }
    // } else {
    //   // this.mobile.markAsDirty();
    //   // this.mobile.updateValueAndValidity();
    //   // this.captcha.markAsDirty();
    //   // this.captcha.updateValueAndValidity();
    //   // if (this.mobile.invalid || this.captcha.invalid) {
    //   //   return;
    //   // }
    // }

    // 默认配置中对所有HTTP请求都会强制 [校验](https://ng-alain.com/auth/getting-started) 用户 Token
    // 然一般来说登录请求不需要校验，因此可以在请求URL加上：`/login?_allow_anonymous=true` 表示不触发用户 Token 校验
    // 1. 调登录接口
    // var tenantCode = (await this.tenant$.toPromise())?.code;
    // var tokenResponse = await this.oauthService.fetchTokenUsingPasswordFlow(this.userName.value.trim(), this.password.value.trim());
    this.apollo
      .mutate({
        mutation: AuthenticateGql,
        variables: {
          input: {
            userIdentifier: this.userName.value.trim(),
            password: this.password.value.trim(),
            // tenantCode,
          },
        },
      })
      .subscribe(
        async res => {
          const authResult = res.data.authenticate;
          if (!authResult.token) {
            this.error = res.errors?.firstOrDefault()?.message;
            return;
          } else {
            // 设置用户Token信息,过期时间
            this.oauthService.initCodeFlow(undefined, { access_token: authResult.token });
            // 重新获取 StartupService 内容，我们始终认为应用信息一般都会受当前用户授权范围而影响
            // await this.startupSrv.load();
            // try {
            //   let url = this.tokenService.referrer!.url || "/";
            //   if (url.includes("/passport")) {
            //     url = "/";
            //   }
            //   this.router.navigateByUrl(url).then(() => {
            //     clearHistory();
            //   });
            // } catch (error) {
            //   this.submitting = false;
            // }
          }
        },
        error => {
          this.submitting = false;
        },
      );
  }

  // #region social

  open(type: `${LoginProviderEnum}`, openType: SocialOpenType = "href"): void {
    let url = ``;
    let callback = ``;
    // tslint:disable-next-line: prefer-conditional-expression
    callback = new URL(`/passport/callback/${type}`, environment.appUrl).toString();
    switch (type) {
      // case "auth0":
      //   url = `//cipchk.auth0.com/login?client=8gcNydIDzGBYxzqV0Vm1CX_RXH-wsWo5&redirect_uri=${encodeURIComponent(callback)}`;
      //   break;
      // case "github":
      //   url = `//github.com/login/oauth/authorize?client_id=9d6baae4b04a23fcafa2&response_type=code&redirect_uri=${encodeURIComponent(
      //     callback,
      //   )}`;
      //   break;
      // case "weibo":
      //   url = `https://api.weibo.com/oauth2/authorize?client_id=1239507802&response_type=code&redirect_uri=${encodeURIComponent(callback)}`;
      //   break;
      case "Geex":
        this.oauthService.initCodeFlow(undefined, { redirect_uri: encodeURIComponent(callback) });
        break;
    }
    if (openType === "window") {
      this.socialService
        .login(url, "/", {
          type: "window",
        })
        .subscribe(res => {
          if (res) {
            this.settingsService.setUser(res);
            this.router.navigateByUrl("/");
          }
        });
    } else {
      this.socialService.login(url, "/", {
        type: "href",
      });
    }
  }

  // #endregion

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
    await this.modalHelper.create(TenantSwitcherComponent).toPromise();
  }
}
