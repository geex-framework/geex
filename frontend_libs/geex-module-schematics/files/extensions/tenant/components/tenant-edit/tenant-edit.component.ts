import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { Apollo } from "apollo-angular";
import { NzModalRef } from "ng-zorro-antd/modal";

import { editTenant, createTenant } from "../../graphql/operations.gql";
import { SharedModule } from "@/shared/shared.module";

@Component({
  selector: "app-tenant-edit",
  templateUrl: "./tenant-edit.component.html",
  styles: [],
  standalone: true,
  imports: [SharedModule],
})
export class TenantEditComponent implements OnInit {
  constructor(
    private modalRef: NzModalRef,
    private apollo: Apollo,
  ) {}
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
          mutation: createTenant,
          variables: {
            code: this.code,
            name: this.name,
          },
        })
        .firstValuePromise();
    } else {
      await this.apollo
        .mutate({
          mutation: editTenant,
          variables: {
            code: this.code,
            name: this.name,
          },
        })
        .firstValuePromise();
    }
    this.modalRef.close(true);
  }
}
