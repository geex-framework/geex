import { Component, Inject, ViewEncapsulation } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { NzMessageService } from "ng-zorro-antd/message";
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
  constructor(
    route: ActivatedRoute,
    public msg: NzMessageService,
  ) { }
}
