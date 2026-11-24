import { ChangeDetectionStrategy, Component, computed, effect, inject, input, untracked } from "@angular/core";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzIconModule } from "ng-zorro-antd/icon";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzToolTipModule } from "ng-zorro-antd/tooltip";
import { FavoritesService } from "./favorites.service";
import { injectFavoriteLabels } from "./favorites.i18n";

@Component({
  selector: "geex-favorite-button",
  standalone: true,
  imports: [NzButtonModule, NzIconModule, NzToolTipModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { "[hidden]": "!visible()" },
  template: `@if (visible()) {
    @let variant = appearance();
    @let status = state();
    <button nz-button type="button" [nzType]="buttonType()" [nzLoading]="status?.pending ?? false"
      nz-tooltip [nzTooltipTitle]="variant === 'link' ? null : actionLabel()"
      [class.favorite-icon]="variant === 'icon'" [class.favorite-detail]="variant === 'default'"
      [attr.aria-label]="variant === 'icon' ? actionLabel() : null"
      [attr.aria-pressed]="status?.isFavorite ?? false" (click)="toggle($event)">
      @if (variant === 'link') {
        <span aria-hidden="true">{{ status?.isFavorite ? '★' : '☆' }}</span>
      } @else {
        <span nz-icon [nzType]="status?.error ? 'exclamation-circle' : 'star'"
          [nzTheme]="status?.isFavorite && !status?.error ? 'fill' : 'outline'" aria-hidden="true"></span>
      }
      @if (variant !== 'icon') {
        <span>{{ variant === 'default' && status?.isFavorite && !status?.error ? (labels().saved ?? actionLabel()) : actionLabel() }}</span>
      }
    </button>
  }`,
  styles: [`
    .favorite-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 26px;
      height: 26px;
      padding: 0;
      font-size: 16px;
      vertical-align: middle;
    }
    .favorite-icon[aria-pressed="false"] > [nz-icon] { opacity: .65; }
    .favorite-icon:hover, .favorite-icon:focus-visible {
      background-color: color-mix(in srgb, currentColor 8%, transparent);
    }
    .favorite-detail { min-inline-size: 7.5em; }
    .favorite-detail[aria-pressed="true"] {
      border-color: color-mix(in srgb, currentColor 45%, transparent);
      background-color: color-mix(in srgb, currentColor 7%, transparent);
    }
    .favorite-icon:focus-visible, .favorite-detail:focus-visible {
      outline: 2px solid currentColor;
      outline-offset: 1px;
    }
  `],
})
export class FavoriteButtonComponent {
  readonly entityType = input.required<string>();
  readonly entityId = input<string | null | undefined>();
  readonly appearance = input<"default" | "link" | "icon">("default");
  readonly favorites = inject(FavoritesService, { optional: true });
  private readonly message = inject(NzMessageService);
  readonly labels = injectFavoriteLabels();
  readonly visible = computed(() => !!this.entityId() && !!this.favorites?.enabled(this.entityType()));
  readonly state = computed(() => this.favorites?.state(this.entityType(), this.entityId() ?? ""));
  readonly buttonType = computed(() => {
    if (this.appearance() === "link" || this.state()?.isFavorite) return "link";
    return this.appearance() === "icon" ? "text" : "default";
  });
  readonly actionLabel = computed(() => {
    if (this.state()?.error) return this.labels().retry;
    return this.state()?.isFavorite ? this.labels().remove : this.labels().add;
  });

  constructor() {
    effect(() => {
      const type = this.entityType();
      const id = this.entityId();
      const visible = this.visible();
      if (visible && id) untracked(() => this.favorites?.requestStatus(type, id));
    });
  }

  async toggle(event: Event): Promise<void> {
    event.stopPropagation();
    const id = this.entityId();
    if (!id || !this.favorites || this.state()?.pending) return;
    if (!this.state()?.loaded || this.state()?.error) { this.favorites.requestStatus(this.entityType(), id, true); return; }
    try { await this.favorites.setFavorite(this.entityType(), id, !this.state()?.isFavorite); }
    catch { this.message.error(this.labels().failed); }
  }
}
