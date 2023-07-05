import { Component, Inject } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { DA_SERVICE_TOKEN, ITokenService } from "@delon/auth";
import { NzMessageService } from "ng-zorro-antd/message";

@Component({
  selector: "passport-register-result",
  templateUrl: "./register-result.component.html",
})
export class UserRegisterResultComponent {
  constructor(route: ActivatedRoute, public msg: NzMessageService, @Inject(DA_SERVICE_TOKEN) private tokenService: ITokenService) {
    let token = route.snapshot.queryParams.token;
    tokenService.set({
      token,
    });
  }
}
