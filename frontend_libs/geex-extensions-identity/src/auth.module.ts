import { Injector, signal } from "@angular/core";
import { Apollo } from "apollo-angular";
import { OAuthService } from "angular-oauth2-oidc";
import gql from "graphql-tag";
import { firstValueFrom, map } from "rxjs";
import { guardedSignal } from "./guarded-signal";
import type { AuthModule, User } from "./types";

const GQL_FEDERATE_AUTH = gql`mutation federateAuthenticate(
  $code: String!
  $loginProvider: LoginProviderEnum!
) {
  federateAuthenticate(
    request: { code: $code, loginProvider: $loginProvider }
  ) {
    token
    loginProvider
    userId
    name
    user {
      id
      username
      nickname
      phoneNumber
      email
      isEnable
      createdOn
      ... on IUser {
        roleNames
        roleIds
        permissions
        orgCodes
        avatarFileId
        orgs {
          allParentOrgs {
            code
            name
          }
          name
          code
        }
        claims {
          claimType
          claimValue
        }
        avatarFile {
          id
          createdOn
          fileSize
          mimeType
          storageType
          fileName
          md5
          url
        }
      }
    }
  }
}
`;

export function createAuthModule(injector: Injector): AuthModule {
  const _user = signal<User | null>(null);
  let _initialized = false;
  let _initPromise: Promise<void> | null = null;
  const module = {
    init: (force = false) => {
      if (force) {
        _initPromise = null;
        _initialized = false;
      }
      if (!_initPromise) {
        _initPromise = (async () => {
          try {
            const userData = await module.loadUserData();
            _user.set(userData ?? null);
            _initialized = true;
          } catch (err) {
            console.error(err);
          }
        })();
      }
      return _initPromise;
    },
    user: guardedSignal(_user, () => _initialized),
    async loadUserData(): Promise<User | undefined> {
      const oAuthService = injector.get(OAuthService);
      try {
        await oAuthService.loadDiscoveryDocument();
        if (!oAuthService.hasValidAccessToken()) {
          return undefined;
        }
        await oAuthService.loadUserProfile();
        const profile = oAuthService.getIdentityClaims() as { login_provider: string } | null;
        if (profile) {
          type FederateAuthResponse = { federateAuthenticate: { user: User } };
          const result = await firstValueFrom(
            injector
              .get(Apollo)
              .mutate<FederateAuthResponse>({
                mutation: GQL_FEDERATE_AUTH,
                variables: {
                  code: oAuthService.getAccessToken(),
                  loginProvider: profile.login_provider,
                },
              })
              .pipe(map(res => res?.data?.federateAuthenticate.user)),
          );
          return result;
        }
      } catch (err) {
        console.error(err);
      }
      return undefined;
    },
  };
  return module as AuthModule;
}
