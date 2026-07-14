import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute } from "@angular/router";
import { Apollo } from "apollo-angular";
import { gql } from "graphql-tag";
import { firstValueFrom } from "rxjs";
import { CONFIRM_MOCK_WECHAT_AUTHORIZATION, GET_MOCK_WECHAT_AUTHORIZATION_STATUS } from "../graphql";

const AUTHORIZE_PROFILES = gql`
  query mockWechatAuthorizeProfiles($token: String!) {
    mockWechatAuthorizeProfiles(token: $token) {
      id
      openId
      nickname
      avatar
    }
  }
`;

type AuthorizeProfile = {
  id: string;
  openId: string;
  nickname: string;
  avatar?: string | null;
};

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:16px;font-family:sans-serif;max-width:480px;margin:0 auto">
      <h2>Confirm Mock WeChat Login</h2>
      @if (loading()) {
        <p>Loading profiles...</p>
      }
      @if (error()) {
        <p style="color:red">{{ error() }}</p>
      }
      @if (done()) {
        <p style="color:green">Confirmed. Return to the desktop browser.</p>
      }
      @if (!done() && !loading()) {
        @for (p of profiles(); track p.id) {
          <button
            type="button"
            style="display:block;width:100%;margin:8px 0;padding:12px"
            (click)="confirm(p.id)"
          >
            {{ p.nickname }} ({{ p.openId }})
          </button>
        }
      }
    </div>
  `,
})
export class MockWechatAuthorizePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly apollo = inject(Apollo);

  token = "";
  profiles = signal<AuthorizeProfile[]>([]);
  error = signal("");
  done = signal(false);
  loading = signal(true);

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get("token") ?? "";
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set("");
    try {
      if (!this.token) {
        this.error.set("Missing authorization token");
        return;
      }

      await firstValueFrom(
        this.apollo.query({
          query: GET_MOCK_WECHAT_AUTHORIZATION_STATUS,
          variables: { token: this.token },
          fetchPolicy: "network-only",
        }),
      );

      const result = await firstValueFrom(
        this.apollo.query<{ mockWechatAuthorizeProfiles: AuthorizeProfile[] }>({
          query: AUTHORIZE_PROFILES,
          variables: { token: this.token },
          fetchPolicy: "network-only",
        }),
      );

      const list = result.data?.mockWechatAuthorizeProfiles ?? [];
      this.profiles.set(list);
      if (!list.length) {
        this.error.set("No enabled mock WeChat profiles. Create one at /mocking/wechat as SuperAdmin.");
      }
    } catch (err: any) {
      this.error.set(err?.message ?? "Unable to load authorization");
      this.profiles.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async confirm(profileId: string): Promise<void> {
    this.error.set("");
    try {
      await firstValueFrom(
        this.apollo.mutate({
          mutation: CONFIRM_MOCK_WECHAT_AUTHORIZATION,
          variables: { request: { token: this.token, profileId } },
        }),
      );
      this.done.set(true);
    } catch (err: any) {
      this.error.set(err?.message ?? "Confirm failed");
    }
  }
}
