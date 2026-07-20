import { Component, inject, Injector, OnDestroy, ViewEncapsulation } from "@angular/core";
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { _HttpClient } from "@delon/theme";
import { Apollo } from "apollo-angular";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { NzMessageService } from "ng-zorro-antd/message";

import { GEEX_I18N } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import { registerAndSignIn, sendSmsCaptcha, validateSmsCaptcha } from "../graphql/operations.gql";
import SparkMD5 from "spark-md5";
@Component({
  selector: "auth-register",
  templateUrl: "./register.page.html",
  styleUrls: ["./register.page.scss"],
  standalone: true,
  encapsulation: ViewEncapsulation.None,
  imports: [SharedModule],
})
export class UserRegisterComponent implements OnDestroy {
  readonly I18N = inject(GEEX_I18N) as any;
  captchaKey: string;
  constructor(
    fb: FormBuilder,
    private router: Router,
    public http: _HttpClient,
    public msg: NzMessageService,
    public apollo: Apollo,
  ) {
    this.form = fb.group({
      mail: [null, [Validators.required, Validators.email]],
      password: [null, [Validators.required, Validators.minLength(6), UserRegisterComponent.checkPassword.bind(this)]],
      confirm: [null, [Validators.required, Validators.minLength(6), UserRegisterComponent.passwordEquar]],
      mobilePrefix: ["+86"],
      mobile: [null, [Validators.required, Validators.pattern(/^1\d{10}$/)]],
      captcha: [null, [Validators.required]],
    });
  }

  // #region fields

  get mail(): AbstractControl {
    return this.form.controls.mail;
  }
  get password(): AbstractControl {
    return this.form.controls.password;
  }
  get confirm(): AbstractControl {
    return this.form.controls.confirm;
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
  visible = false;
  status = "pool";
  progress = 0;
  passwordProgressMap: { [key: string]: "success" | "normal" | "exception" } = {
    ok: "success",
    pass: "normal",
    pool: "exception",
  };

  // #endregion

  // #region get captcha

  count = 0;
  interval$: any;

  static checkPassword(control: FormControl): NzSafeAny {
    if (!control) {
      return null;
    }
    // eslint-disable-next-line @typescript-eslint/no-this-alias
    const self: any = this;
    self.visible = !!control.value;
    if (control.value && control.value.length > 9) {
      self.status = "ok";
    } else if (control.value && control.value.length > 5) {
      self.status = "pass";
    } else {
      self.status = "pool";
    }

    if (self.visible) {
      self.progress = control.value.length * 10 > 100 ? 100 : control.value.length * 10;
    }
  }

  static passwordEquar(control: FormControl): { equar: boolean } | null {
    if (!control || !control.parent!) {
      return null;
    }
    if (control.value !== control.parent!.get("password")!.value) {
      return { equar: true };
    }
    return null;
  }

  async getCaptcha(): Promise<void> {
    if (this.mobile.invalid) {
      this.mobile.markAsDirty({ onlySelf: true });
      this.mobile.updateValueAndValidity({ onlySelf: true });
      return;
    }
    this.count = 59;
    let res = await this.apollo.mutate({ mutation: sendSmsCaptcha, variables: { phoneOrEmail: this.mobile.value } }).firstValuePromise();
    this.captchaKey = res.data.generateCaptcha.key;
    this.interval$ = setInterval(() => {
      this.count -= 1;
      if (this.count <= 0) {
        clearInterval(this.interval$);
      }
    }, 1000);
  }

  // #endregion

  async submit(): Promise<void> {
    this.error = "";
    Object.keys(this.form.controls).forEach(key => {
      this.form.controls[key].markAsDirty();
      this.form.controls[key].updateValueAndValidity();
    });
    if (this.form.invalid) {
      return;
    }

    const data = this.form.value;
    let validateRes = await this.apollo
      .mutate({
        mutation: validateSmsCaptcha,
        variables: {
          captchaKey: this.captchaKey,
          captchaCode: this.captcha.value,
        },
      })
      .firstValuePromise();
    if (validateRes.data.validateCaptcha) {
      let res = await this.apollo
        .mutate({
          mutation: registerAndSignIn,
          variables: {
            registerRequest: {
              password: SparkMD5.hash(this.password.value),
              phoneNumber: this.mobile.value,
              username: this.mobile.value,
            },
            authenticateRequest: {
              password: SparkMD5.hash(this.password.value),
              userIdentifier: this.mobile.value,
            },
          },
        })
        .firstValuePromise();

      if (res.data.register && res.data.authenticate) {
        this.router.navigate(["auth", "register-result"], { queryParams: { token: res.data.authenticate.token } });
      }
    }
  }

  ngOnDestroy(): void {
    if (this.interval$) {
      clearInterval(this.interval$);
    }
  }
}
