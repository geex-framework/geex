import { Component } from "@angular/core";
import { Store } from "@ngxs/store";
import { OAuthService } from "angular-oauth2-oidc";

import { environment } from "../../../environments/environment";

@Component({
  selector: "exception-403",
  template: `
    <exception type="403" style="min-height: 500px; height: 80%;">
      <button nz-button *ngIf="this.loggedIn" [nzType]="'primary'" [routerLink]="'/personal-center'">个人中心</button>
      <button nz-button (click)="backToEnterprise()">返回企业</button>
    </exception>
  `,
})
export class Exception403Component {
  /**
   *
   */
  loggedIn: boolean;
  constructor(private oauthService: OAuthService, private store: Store) {
    this.loggedIn = oauthService.hasValidAccessToken();
  }
  backToEnterprise() {
    this.oauthService.logOut(true);
  }
}
