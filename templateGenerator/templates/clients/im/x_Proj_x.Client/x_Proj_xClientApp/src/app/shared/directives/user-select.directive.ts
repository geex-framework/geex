import { Directive, Input } from "@angular/core";
import { NgModel } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { NzDestroyService } from "ng-zorro-antd/core/services";
import { NzOptionComponent, NzOptionGroupComponent, NzSelectComponent } from "ng-zorro-antd/select";
import { BehaviorSubject } from "rxjs";
import { debounceTime, map } from "rxjs/operators";

import { UserMenusGql, UserMenusQuery, UserMenusQueryVariables } from "../graphql/.generated/type";

@Directive({
  selector: "[user-select]",
})
export class UserSelectDirective {
  users: Array<{ label: string; value: string }>;
  inputValue$ = new BehaviorSubject("");

  constructor(private host: NzSelectComponent, private apollo: Apollo) {
    this.host.nzDropdownMatchSelectWidth = false;
    this.host.nzOptionOverflowSize = 10;
    this.host.nzAllowClear = true;
  }
  async ngOnInit(): Promise<void> {
    this.inputValue$
      .asObservable()
      .pipe(debounceTime(600))
      .subscribe(async filter => {
        await this.search(filter);
      });
  }
  async search(filter: string = "") {
    this.host.nzLoading = true;
    this.users = await this.apollo
      .query<UserMenusQuery, UserMenusQueryVariables>({
        query: UserMenusGql,
        variables: {},
      })
      .pipe(map(x => x.data.users.items.map(y => ({ label: y.username + (y.nickname !== null ? y.nickname : ""), value: y.id }))))
      .toPromise();
    this.updateHostControl();
    this.host.nzLoading = false;
  }

  private updateHostControl() {
    const defaultItem = new NzOptionComponent(new NzOptionGroupComponent(), new NzDestroyService());
    defaultItem.nzLabel = "全部";
    defaultItem.nzValue = "";
    this.host.listOfNzOptionComponent?.reset([
      // defaultItem,
      ...this.users.map(x => {
        const newItem = new NzOptionComponent(new NzOptionGroupComponent(), new NzDestroyService());
        newItem.nzLabel = `${x.label}`;
        newItem.nzValue = x.value;
        return newItem;
      }),
    ]);
    this.host.listOfNzOptionComponent?.notifyOnChanges();
  }
}
