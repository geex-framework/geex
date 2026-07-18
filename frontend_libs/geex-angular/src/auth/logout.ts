import { inject, Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { OAuthService } from "angular-oauth2-oidc";
import { Apollo } from "apollo-angular";
import gql from "graphql-tag";
import type { DocumentNode } from "graphql";
import { firstValueFrom } from "rxjs";

import { ACLService } from "@delon/acl";
import { SettingsService } from "@delon/theme";

import { GEEX_AFTER_LOGIN_NAVIGATE, GEEX_LOGIN_PATH } from "../http/tokens";
import { GEEX_CANCEL_AUTHENTICATION_DOCUMENT } from "./tokens";

export const cancelAuthenticationMutation = gql`
  mutation cancelAuthenticate {
    cancelAuthentication
  }
` as DocumentNode;

@Injectable({ providedIn: "root" })
export class GeexAuthLogout {
  private readonly apollo = inject(Apollo);
  private readonly oauth = inject(OAuthService);
  private readonly settings = inject(SettingsService);
  private readonly acl = inject(ACLService);
  private readonly router = inject(Router);
  private readonly loginPath = inject(GEEX_LOGIN_PATH);
  private readonly afterLoginNavigate = inject(GEEX_AFTER_LOGIN_NAVIGATE);
  private readonly cancelDocument = inject(GEEX_CANCEL_AUTHENTICATION_DOCUMENT, { optional: true });

  async logout(): Promise<void> {
    const mutation = this.cancelDocument ?? cancelAuthenticationMutation;
    await firstValueFrom(this.apollo.mutate({ mutation }));
    this.settings.setUser({});
    this.acl.set({});
    this.oauth.logOut();
    await this.router.navigateByUrl(this.loginPath).then(() => {
      this.afterLoginNavigate();
    });
  }
}
