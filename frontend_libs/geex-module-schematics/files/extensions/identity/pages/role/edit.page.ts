import { Component, Injector } from "@angular/core";
import { _HttpClient } from "@delon/theme";

import { RoutedComponent, RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";

import { AppPermission, AuthorizeTargetType } from "@/gql";
import type { CreateRoleRequest } from "@/gql";
import { roleById, createRole, roleLists } from "../../graphql/role.operations.gql";
import type { RoleDetail } from "../../graphql/role.operations.gql";
import { authorize } from "../../graphql/role.operations.gql";

export type RoleEditComponentParams = {
  id: string;
  roleName: string;
};

@Component({
  selector: "app-role-edit",
  templateUrl: "./edit.page.html",
  standalone: true,
  imports: [SharedModule],
})
export class RoleEditComponent extends RoutedComponent<RoleEditComponentParams> {
  allowedPermissions: string[];
  originalAllowedPermissions: string[];
  id?: string;
  entity?: Hint<RoleDetail>;
  routeParamsMappings: RouteParamsMappings<RoleEditComponentParams> = {
    id: { position: "pathParams", default: null },
    roleName: { position: "queryParams", default: null },
  };

  override async onRouted(params: RoleEditComponentParams) {
    this.id = params.id;
    if (params.id) {
      let res = await this.apollo
        .query({
          query: roleById,
          variables: {
            id: params.id,
          },
          fetchPolicy: "no-cache",
        })
        .firstValuePromise();
      let entity = res.data.roles.items[0];
      this.originalAllowedPermissions = entity.permissions;
      this.allowedPermissions = entity.permissions as AppPermission[];
      this.entity = entity;
    } else {
      this.originalAllowedPermissions = [];
      this.allowedPermissions = [];
      this.entity = undefined;
    }
  }

  async close() {
    if (this.allowedPermissions.sequenceEqual(this.originalAllowedPermissions)) {
      this.location.back();
    } else {
      this.nzModalSrv.confirm({
        nzTitle: "当前页面内容未保存，确定离开？",
        nzOnOk: () => {
          // this.router.navigate(["/identity/role"], { relativeTo: this.route });
          this.location.back();
        },
      });
    }
  }

  async submit(): Promise<void> {
    if (!!!this.id) {
      const newRole = await this.apollo
        .mutate({
          mutation: createRole,
          variables: {
            request: {
              roleName: this.paramsForm.value.roleName,
              roleCode: this.paramsForm.value.roleName,
            } as CreateRoleRequest,
          },
          refetchQueries: [roleLists],
        })
        .firstValuePromise();
      this.paramsForm.controls.id.setValue(newRole.data.createRole.id);
    }

    await this.apollo
      .mutate({
        mutation: authorize,
        variables: {
          request: {
            authorizeTargetType: AuthorizeTargetType.Role,
            target: this.paramsForm.value.id,
            allowedPermissions: this.allowedPermissions as AppPermission[],
          },
          // severity: value.severity,
        },
      })
      .firstValuePromise();
    this.msgSrv.success("修改成功");
    // await this.router.navigate(["/identity/role"], { relativeTo: this.route });
    await this.router.navigate(["/identity/role"], { relativeTo: this.route, replaceUrl: true, forceReload: true });
  }
}


