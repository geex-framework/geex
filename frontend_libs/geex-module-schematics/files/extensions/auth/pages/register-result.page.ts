import { Component, inject, ViewEncapsulation } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { NzMessageService } from "ng-zorro-antd/message";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "auth-register-result",
  templateUrl: "./register-result.page.html",
  styleUrls: ["./register-result.page.scss"],
  standalone: true,
  encapsulation: ViewEncapsulation.None,
  imports: [SharedModule],
})
export class UserRegisterResultComponent {
  readonly I18N = inject(GEEX_I18N) as any;

  constructor(
    route: ActivatedRoute,
    public msg: NzMessageService,
  ) { }
}
