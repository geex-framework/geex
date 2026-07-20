import { Component, OnInit, inject, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { gql } from "graphql-tag";
import { firstValueFrom } from "rxjs";
import { MOCK_WECHAT_PROFILES } from "@geexcode/geex-extensions-mocking";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

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
  imports: [SharedModule, FormsModule],
  template: `
    <page-header [title]="I18N.Mocking.wechat.title" [autoBreadcrumb]="true" [extra]="phExtra">
      <form nz-form nzLayout="inline">
        <nz-form-item>
          <nz-form-label>{{ I18N.Mocking.wechat.openId }}</nz-form-label>
          <nz-form-control>
            <input nz-input [(ngModel)]="openId" name="openId" [placeholder]="I18N.Mocking.wechat.openId" required />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label>{{ I18N.Mocking.wechat.nickname }}</nz-form-label>
          <nz-form-control>
            <input nz-input [(ngModel)]="nickname" name="nickname" [placeholder]="I18N.Mocking.wechat.nickname" required />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label>{{ I18N.Mocking.wechat.unionId }}</nz-form-label>
          <nz-form-control>
            <input nz-input [(ngModel)]="unionId" name="unionId" [placeholder]="I18N.Mocking.wechat.unionId" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <button nz-button nzType="default" type="button" (click)="reload()">
            <i nz-icon nzType="reload" nzTheme="outline"></i>{{ I18N.Common.action.refresh }}
          </button>
        </nz-form-item>
      </form>
      <ng-template #phExtra>
        <button nz-button nzType="primary" type="button" (click)="create()">
          <i nz-icon nzType="plus"></i>{{ I18N.Mocking.wechat.create }}
        </button>
      </ng-template>
    </page-header>
    <nz-card>
      @if (error()) {
        <nz-alert nzType="error" [nzMessage]="error()" class="mb-md"></nz-alert>
      }

      <nz-table #table [nzData]="profiles()" [nzLoading]="loading()" [nzFrontPagination]="false" [nzShowPagination]="false">
        <thead>
          <tr>
            <th>{{ I18N.Mocking.wechat.openId }}</th>
            <th>{{ I18N.Mocking.wechat.nickname }}</th>
            <th>{{ I18N.Mocking.wechat.unionId }}</th>
            <th>{{ I18N.Mocking.wechat.enabled }}</th>
          </tr>
        </thead>
        <tbody>
          @for (p of table.data; track p.id) {
            <tr>
              <td>{{ p.openId }}</td>
              <td>{{ p.nickname }}</td>
              <td>{{ p.unionId }}</td>
              <td>{{ p.enabled }}</td>
            </tr>
          }
        </tbody>
      </nz-table>
    </nz-card>
  `,
})
export class MockingWechatProfilesPage implements OnInit {
  I18N = inject(GEEX_I18N) as any;
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
      this.error.set(err?.message ?? this.I18N.Mocking.wechat.loadFailed);
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
      this.error.set(err?.message ?? this.I18N.Mocking.wechat.createFailed);
    }
  }
}
