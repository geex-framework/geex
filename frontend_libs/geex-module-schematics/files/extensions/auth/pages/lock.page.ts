import { Component, Inject, ViewEncapsulation } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { SettingsService, User } from "@delon/theme";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "auth-lock",
  templateUrl: "./lock.page.html",
  styleUrls: ["./lock.page.scss"],
  standalone: true,
  imports: [SharedModule],
  encapsulation: ViewEncapsulation.None,
})
export class UserLockComponent {
  f: FormGroup;

  get user(): User {
    return this.settings.user;
  }

  constructor(
    fb: FormBuilder,
    private settings: SettingsService,
    private router: Router,
  ) {
    this.f = fb.group({
      password: [null, Validators.required],
    });
  }

  submit(): void {
    // tslint:disable-next-line:forin
    for (const i in this.f.controls) {
      this.f.controls[i].markAsDirty();
      this.f.controls[i].updateValueAndValidity();
    }
    if (this.f.valid) {
      this.router.navigate(["dashboard"]);
    }
  }
}
