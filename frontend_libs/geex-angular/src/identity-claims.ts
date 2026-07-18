export interface IdentityClaims {
  nbf: number;
  exp: number;
  iss: string;
  aud: string;
  nonce: string;
  iat: number;
  at_hash: string;
  s_hash: string;
  sid: string;
  sub: string;
  auth_time: number;
  idp: string;
  amr: string[];
  picture: string;
  role: string;
  email: string;
  phone_number: string;
  name: string;
  nickname: string;
  __tenant: string;
  preferred_username: string;
  email_verified: boolean;
  login_provider: string;
}

declare module "angular-oauth2-oidc" {
  interface OAuthService {
    getIdentityClaims(this: this): IdentityClaims;
  }
}
