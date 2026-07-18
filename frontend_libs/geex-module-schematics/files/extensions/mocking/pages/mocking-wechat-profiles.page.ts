import { Component, OnInit, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { gql } from "graphql-tag";
import { firstValueFrom } from "rxjs";
import { MOCK_WECHAT_PROFILES } from "@geexcode/geex-extensions-mocking";

const CREATE_PROFILE = gql`
  mutation createMockWechatProfile($request: CreateMockWechatProfileRequest!) {
    createMockWechatProfile(request: $request) {
      id
      openId
      nickname
      unionId
      avatar
      enabled
    }
  }
`;

type MockWechatProfileRow = {
  id: string;
  openId: string;
  nickname: string;
  unionId?: string | null;
  enabled: boolean;
};

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="padding:16px;font-family:sans-serif">
      <h2>Mock WeChat Profiles</h2>
      <form (ngSubmit)="create()" style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:16px">
        <input [(ngModel)]="openId" name="openId" placeholder="openId" required />
        <input [(ngModel)]="nickname" name="nickname" placeholder="nickname" required />
        <input [(ngModel)]="unionId" name="unionId" placeholder="unionId" />
        <button type="submit">Create</button>
      </form>
      @if (loading()) {
        <p>Loading...</p>
      }
      @if (error()) {
        <p style="color:red">{{ error() }}</p>
      }
      <table border="1" cellpadding="6" style="border-collapse:collapse;width:100%">
        <thead>
          <tr><th>OpenId</th><th>Nickname</th><th>UnionId</th><th>Enabled</th></tr>
        </thead>
        <tbody>
          @for (p of profiles(); track p.id) {
            <tr>
              <td>{{ p.openId }}</td>
              <td>{{ p.nickname }}</td>
              <td>{{ p.unionId }}</td>
              <td>{{ p.enabled }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class MockingWechatProfilesPage implements OnInit {
  private readonly apollo = inject(Apollo);

  profiles = signal<MockWechatProfileRow[]>([]);
  loading = signal(false);
  error = signal("");
  openId = "";
  nickname = "";
  unionId = "";

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set("");
    try {
      const result = await firstValueFrom(
        this.apollo.query<{ mockWechatProfiles: MockWechatProfileRow[] }>({
          query: MOCK_WECHAT_PROFILES,
          fetchPolicy: "network-only",
        }),
      );
      this.profiles.set(result.data?.mockWechatProfiles ?? []);
    } catch (err: any) {
      this.error.set(err?.message ?? "Failed to load profiles");
      this.profiles.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  async create(): Promise<void> {
    this.error.set("");
    try {
      await firstValueFrom(
        this.apollo.mutate({
          mutation: CREATE_PROFILE,
          variables: {
            request: {
              openId: this.openId,
              nickname: this.nickname,
              unionId: this.unionId || null,
              enabled: true,
            },
          },
        }),
      );
      this.openId = "";
      this.nickname = "";
      this.unionId = "";
      await this.reload();
    } catch (err: any) {
      this.error.set(err?.message ?? "Failed to create profile");
    }
  }
}
