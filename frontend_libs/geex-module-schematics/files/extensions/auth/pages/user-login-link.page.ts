import { Component, OnInit, signal, Signal, ViewEncapsulation } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzTabChangeEvent } from "ng-zorro-antd/tabs";
import SparkMD5 from "spark-md5";
import type { Tenant } from "@geexcode/geex-extensions-identity";
import { geex, rison } from "@geexcode/geex-angular";
import { ModalHelper } from "@delon/theme";
import { SharedModule } from "@/shared/shared.module";
import { TenantSwitcherComponent } from "@/modules/tenant/components/tenant-switcher/tenant-switcher.component";
import { authenticate } from "../graphql/operations.gql";

@Component({
  selector: "auth-user-login-link",
  templateUrl: "./user-login-link.page.html",
  styleUrls: ["./user-login-link.page.scss"],
  standalone: true,
  imports: [SharedModule],
  encapsulation: ViewEncapsulation.None,
})
export class UserLoginLinkPage implements OnInit {
  geex = geex;
  tenant$: Signal<Tenant>;
  userLoginLinkToken = "";
  displayName = "";
  submitting = signal(false);
  error = "";
  type = 0;
  linkForm: FormGroup;
  registerForm: FormGroup;

  constructor(
    fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private apollo: Apollo,
    private msg: NzMessageService,
    private modalHelper: ModalHelper,
  ) {
    this.tenant$ = geex.tenant.current;
    this.linkForm = fb.group({
      userName: [null, [Validators.required]],
      password: [null, [Validators.required]],
    });
    this.registerForm = fb.group({
      userName: [null, [Validators.required]],
      password: [null, [Validators.required, Validators.minLength(6)]],
      email: [null],
      phoneNumber: [null],
    });
  }

  ngOnInit(): void {
    this.userLoginLinkToken = rison.decode_query_param(
      this.route.snapshot.queryParamMap.get("userLoginLinkToken"),
    );
    this.displayName = rison.decode_query_param(this.route.snapshot.queryParamMap.get("displayName"));
    if (!this.userLoginLinkToken) {
      this.msg.error("缺少 UserLoginLinkToken, 请重新扫码登录");
      void this.router.navigate(["/auth/login"]);
    }
  }

  switch({ index }: NzTabChangeEvent): void {
    this.type = index!;
  }

  async showTenantSwitcher() {
    const modal = this.modalHelper.create(TenantSwitcherComponent);
    await modal.firstValuePromise();
  }

  async linkExisting(): Promise<void> {
    if (this.linkForm.invalid) {
      Object.values(this.linkForm.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity({ onlySelf: true });
      });
      return;
    }
    this.submitting.set(true);
    this.error = "";
    try {
      const authRes = await this.apollo
        .mutate({
          mutation: authenticate,
          variables: {
            request: {
              userIdentifier: this.linkForm.value.userName.trim(),
              password: SparkMD5.hash(this.linkForm.value.password.trim()),
            },
          },
        })
        .firstValuePromise();
      const localToken = authRes.data?.authenticate?.token;
      if (!localToken) {
        this.error = "本地登录失败";
        return;
      }

      const session = await geex.wechatAuth.linkLogin(this.userLoginLinkToken, {
        headers: {
          Authorization: `Local ${localToken}`,
        },
      });
      if (!session?.token) {
        this.error = "关联登录失败";
        return;
      }
      geex.wechatAuth.establishSession({ token: session.token, returnUrl: "/" });
    } catch (e: any) {
      this.error = e?.message ?? String(e);
    } finally {
      this.submitting.set(false);
    }
  }

  async registerAndLink(): Promise<void> {
    if (this.registerForm.invalid) {
      Object.values(this.registerForm.controls).forEach(c => {
        c.markAsDirty();
        c.updateValueAndValidity({ onlySelf: true });
      });
      return;
    }
    this.submitting.set(true);
    this.error = "";
    try {
      const session = await geex.wechatAuth.registerAndLinkLogin({
        userLoginLinkToken: this.userLoginLinkToken,
        username: this.registerForm.value.userName.trim(),
        password: SparkMD5.hash(this.registerForm.value.password.trim()),
        email: this.registerForm.value.email?.trim() || undefined,
        phoneNumber: this.registerForm.value.phoneNumber?.trim() || undefined,
        nickname: this.displayName || undefined,
      });
      if (!session.token) {
        this.error = "注册并关联失败";
        return;
      }
      geex.wechatAuth.establishSession({ token: session.token, returnUrl: "/" });
    } catch (e: any) {
      this.error = e?.message ?? String(e);
    } finally {
      this.submitting.set(false);
    }
  }
}
