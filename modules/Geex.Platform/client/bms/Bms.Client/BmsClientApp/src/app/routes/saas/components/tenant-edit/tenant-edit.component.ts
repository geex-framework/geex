import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzModalRef } from "ng-zorro-antd/modal";

import { EditTenantGql, CreateTenantGql } from "../../../../shared/graphql/.generated/type";

@Component({
  selector: "app-tenant-edit",
  templateUrl: "./tenant-edit.component.html",
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantEditComponent implements OnInit {
  constructor(private modalRef: NzModalRef, private apollo: Apollo) {}
  isCreate: boolean;
  name: string;
  code: string;
  ngOnInit(): void {
    if (this.code && this.code.length > 0) {
      this.isCreate = false;
    } else {
      this.isCreate = true;
    }
  }
  async submit() {
    if (this.isCreate) {
      await this.apollo
        .mutate({
          mutation: CreateTenantGql,
          variables: {
            code: this.code,
            name: this.name,
          },
        })
        .toPromise();
    } else {
      await this.apollo
        .mutate({
          mutation: EditTenantGql,
          variables: {
            code: this.code,
            name: this.name,
          },
        })
        .toPromise();
    }
    this.modalRef.close(true);
  }
}
