import { Component, Inject, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { DA_SERVICE_TOKEN, ITokenService, JWTTokenModel } from "@delon/auth";

@Component({
  selector: "layout-passport",
  templateUrl: "./passport.component.html",
  styleUrls: ["./passport.component.scss"],
})
export class LayoutPassportComponent implements OnInit {
  currentYear: number;
  isMobile: boolean = false;
  constructor(@Inject(DA_SERVICE_TOKEN) private tokenService: ITokenService, private route: ActivatedRoute, private router: Router) {
    this.currentYear = new Date().getFullYear();
  }

  ngOnInit(): void {
    const tokenModel = this.tokenService.get<JWTTokenModel>(JWTTokenModel);
    if (
      !(
        this.router.routerState.snapshot.url &&
        this.router.routerState.snapshot.url.toLocaleLowerCase().includes("passport/callback/x_org_x") &&
        tokenModel.token
      )
    ) {
      this.tokenService.clear();
    }
    this.isMobile = window.isMobile;
  }
}
