import { Component, inject } from "@angular/core";
import { RouterLink } from "@angular/router";
import { SharedModule } from "@/shared/shared.module";
import { GEEX_I18N } from "@geexcode/geex-angular";

@Component({
  standalone: true,
  imports: [SharedModule, RouterLink],
  template: `
    <page-header [title]="I18N.Mocking.home.title" [autoBreadcrumb]="true" />
    <nz-card>
      <p class="mb-md">{{ I18N.Mocking.home.description }}</p>
      <div nz-row [nzGutter]="[16, 16]">
        @for (entry of entries; track entry.link) {
          <div nz-col [nzXs]="24" [nzSm]="12" [nzMd]="8">
            <nz-card [nzHoverable]="true" [routerLink]="entry.link" class="mocking-entry-card">
              <nz-card-meta
                [nzTitle]="entry.title"
                [nzDescription]="entry.description"
                [nzAvatar]="avatarTpl"
              ></nz-card-meta>
              <ng-template #avatarTpl>
                <i nz-icon [nzType]="entry.icon" nzTheme="outline" style="font-size: 28px"></i>
              </ng-template>
            </nz-card>
          </div>
        }
      </div>
    </nz-card>
  `,
  styles: [
    `
      .mocking-entry-card {
        height: 100%;
      }
    `,
  ],
})
export class MockingHomePage {
  I18N = inject(GEEX_I18N) as any;

  get entries() {
    return [
      {
        link: "/mocking/wechat",
        icon: "wechat",
        title: this.I18N.Mocking.entries.wechat.title,
        description: this.I18N.Mocking.entries.wechat.description,
      },
      {
        link: "/mocking/sms",
        icon: "mail",
        title: this.I18N.Mocking.entries.sms.title,
        description: this.I18N.Mocking.entries.sms.description,
      },
      {
        link: "/mocking/payments",
        icon: "pay-circle",
        title: this.I18N.Mocking.entries.payments.title,
        description: this.I18N.Mocking.entries.payments.description,
      },
    ];
  }
}
