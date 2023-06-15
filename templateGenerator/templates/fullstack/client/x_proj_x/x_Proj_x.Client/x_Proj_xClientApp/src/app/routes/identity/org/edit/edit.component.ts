import { Component, OnInit, ChangeDetectionStrategy, Input, Injector } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { Apollo } from "apollo-angular";
import { NzNotificationService } from "ng-zorro-antd/notification";

import { CreateOrgGql, CreateOrgInput, OrgBriefFragment } from "../../../../shared/graphql/.generated/type";

import { ModalComponentBase } from "@/app/shared/components";

@Component({
  selector: "app-edit",
  templateUrl: "./edit.component.html",
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrgEditComponent extends ModalComponentBase implements OnInit {
  @Input() org: CreateOrgInput;

  constructor(injector: Injector, private apollo: Apollo, private notify: NzNotificationService) {
    super(injector);
  }

  ngOnInit(): void {
    // todo
    // this.title = this.org?.id ? "重命名" : "新增";
  }
  async submit() {
    // todo 编辑接口待接入
    await this.apollo
      .mutate({
        mutation: CreateOrgGql,
        variables: {
          input: this.org as CreateOrgInput,
        },
      })
      .toPromise();
    this.success(true);
    this.notify.success(`${this.title}成功`, "");
  }
}
