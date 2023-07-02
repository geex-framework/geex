import { Component, Injector, OnInit, ViewChild } from "@angular/core";
import { SFComponent, SFSchema, SFUISchema } from "@delon/form";
import { _HttpClient } from "@delon/theme";
import { deepCopy } from "@delon/util";
import { map } from "rxjs/operators";

import { BusinessComponentBase } from "../../../../shared/components/business.component.base";
import { RoutedComponent } from "@/app/shared/components/routed-components/routed.component.base";
import {
  AuthorizeInput,
  AuthorizeGql,
  AuthorizeTargetType,
  AppPermission,
  CreateRoleInput,
  CreateRoleGql,
  RoleDetailFragment,
} from "../../../../shared/graphql/.generated/type";
import { EditMode } from "../../../../shared/types/common";
import { RoleByIdGql, RoleByIdQuery, RoleByIdQueryVariables } from "./../../../../shared/graphql/.generated/type";

export type RoleEditComponentParams = {
  id: string;
  roleName: string;
};

type RoleEditComponentData = {
  isStatic: boolean;
  authorizeTargetType: AuthorizeTargetType;
};

@Component({
  selector: "app-user-edit",
  templateUrl: "./edit.component.html",
})
export class RoleEditComponent extends RoutedComponent<RoleEditComponentParams, RoleEditComponentData> {
  mode: EditMode;
  allowedPermissions: string[];
  originalAllowedPermissions: string[];
  ui: SFUISchema = {
    "*": {
      spanLabelFixed: 100,
    },
  };

  constructor(private injector: Injector) {
    super(injector);
  }

  close() {
    if (this.originalAllowedPermissions.sequenceEqual(this.allowedPermissions)) {
      this.router.navigate(["/identity/role"], { relativeTo: this.route, replaceUrl: true });
    } else {
      this.nzModalSrv.confirm({
        nzTitle: "当前页面内容未保存，确定离开？",
        nzOnOk: () => {
          // this.router.navigate(["/identity/role"], { relativeTo: this.route });
          this.router.navigate(["/identity/role"], { relativeTo: this.route, replaceUrl: true });
        },
      });
    }
  }

  async fetchData() {
    let params = this.params.value;
    this.mode = params.id ? "edit" : "create";
    let result: RoleEditComponentData;
    if (params.id) {
      let res = await this.apollo
        .query<RoleByIdQuery, RoleByIdQueryVariables>({
          query: RoleByIdGql,
          variables: {
            id: params.id,
          },
        })
        .toPromise();
      let entity = res.data.roles.items[0];
      this.originalAllowedPermissions = deepCopy(entity.permissions);
      setTimeout(() => {
        this.allowedPermissions = entity.permissions;
      });
      result = {
        isStatic: entity.isStatic,
        authorizeTargetType: AuthorizeTargetType.Role,
      };
    } else {
      result = {
        authorizeTargetType: AuthorizeTargetType.Role,
        isStatic: false,
      };
    }

    return result;
  }

  async submit(): Promise<void> {
    if (this.mode === "create") {
      await this.apollo
        .mutate({
          mutation: CreateRoleGql,
          variables: {
            input: {
              roleName: this.params.value.roleName,
            } as CreateRoleInput,
          },
        })
        .toPromise();
    }

    await this.apollo
      .mutate({
        mutation: AuthorizeGql,
        variables: {
          input: {
            authorizeTargetType: AuthorizeTargetType.Role,
            target: this.params.value.id,
            allowedPermissions: this.allowedPermissions as AppPermission[],
          },
          // severity: value.severity,
        },
      })
      .toPromise();
    this.msgSrv.success("修改成功");
    // await this.router.navigate(["/identity/role"], { relativeTo: this.route });
    await this.router.navigate(["/identity/role"], { relativeTo: this.route, replaceUrl: true });
  }
}
