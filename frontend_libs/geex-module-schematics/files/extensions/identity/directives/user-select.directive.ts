import { ChangeDetectorRef, Directive, OnInit, SimpleChange, inject } from "@angular/core";
import { GEEX_I18N } from "@geexcode/geex-angular";
import { Apollo } from "apollo-angular";
import { NzSelectComponent, type NzSelectOptionInterface } from "ng-zorro-antd/select";
import { map } from "rxjs/operators";

import { usersCache } from "../graphql/user.operations.gql";
import type { usersCacheResult, usersCacheVariables } from "../graphql/user.operations.gql";

@Directive({
  selector: "[user-select]",
  // eslint-disable-next-line @angular-eslint/prefer-standalone -- SharedModule 声明并导出此指令.
  standalone: false,
})
export class UserSelectDirective implements OnInit {
  private readonly i18n = inject(GEEX_I18N);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly host = inject(NzSelectComponent);
  private readonly apollo = inject(Apollo);
  private loaded = false;

  constructor() {
    this.host.nzDropdownMatchSelectWidth = false;
    this.host.nzOptionOverflowSize = 10;
    this.host.nzAllowClear = true;
    this.host.nzShowSearch = true;
    if (!this.host.nzPlaceHolder) {
      this.host.nzPlaceHolder = this.i18n.Identity?.widget?.selectUser ?? "Select user";
    }
  }

  ngOnInit(): void {
    void this.ensureOptions();
    this.host.nzOpenChange.subscribe((open: boolean) => {
      if (open) {
        void this.ensureOptions();
      }
    });
  }

  private async ensureOptions(): Promise<void> {
    if (this.loaded && (this.host.nzOptions?.length ?? 0) > 0) {
      return;
    }
    this.host.nzLoading = true;
    try {
      const options = await this.apollo
        .query<usersCacheResult, usersCacheVariables>({
          query: usersCache,
          variables: {},
          fetchPolicy: "cache-first",
        })
        .pipe(
          map(x =>
            (x.data.usersCache ?? [])
              .filter((y): y is NonNullable<typeof y> => y != null)
              .map(
                y =>
                  ({
                    label: `${y.username}${y.nickname ? ` (${y.nickname})` : ""}`,
                    value: y.id,
                  }) satisfies NzSelectOptionInterface,
              ),
          ),
        )
        .firstValuePromise();
      this.applyOptions(options);
      this.loaded = true;
    } finally {
      this.host.nzLoading = false;
      this.cdr.markForCheck();
    }
  }

  private applyOptions(options: NzSelectOptionInterface[]): void {
    const previous = this.host.nzOptions;
    this.host.nzOptions = options;
    this.host.ngOnChanges({
      nzOptions: new SimpleChange(previous, options, previous == null),
    });
  }
}
