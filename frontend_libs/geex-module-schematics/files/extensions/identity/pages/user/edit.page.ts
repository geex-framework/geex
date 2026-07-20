import { Component, inject, Injector, Signal, signal, ViewChild } from "@angular/core";

import { SFComponent, SFSchema, SFSelectWidgetSchema, SFUISchema } from "@delon/form";
import { _HttpClient, ModalHelper } from "@delon/theme";
import { RoutedComponent, RouteParamsMappings } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import type { CreateUserRequest, EditUserRequest, ResetUserPasswordRequest } from "@/gql";
import { OrgTypeEnum } from "@/gql";
import { userById, createUser, editUser, resetUserPassword } from "../../graphql/user.operations.gql";
import { GeexUploadWidgetSchema } from "@/modules/blob-storage/widgets/upload";
import { OrgTreeSelectWidgetSchema } from "@/modules/identity/widgets/org-tree";
import SparkMD5 from "spark-md5";

@Component({
  selector: "app-user-edit",
  templateUrl: "./edit.page.html",
  standalone: true,
  imports: [SharedModule],
})
export class UserEditPage extends RoutedComponent<{ id?: string }> {
  private mh = inject(ModalHelper);
  override routeParamsMappings: RouteParamsMappings<{ id?: string }> = {
    id: { position: "pathParams" },
  };
  override async onRouted(params: { id?: string }): Promise<any> {
    this.id.set(params.id);
    const id = this.id();
    if (id) {
      this.resetUserPasswordRequest.userId = id;
      let res: any = await this.apollo
        .query({
          query: userById,
          variables: {
            id: id,
          },
        })
        .firstValuePromise();
      this.schema.properties.username.readOnly = true;
      let entity = res.data.users.items[0];
      this.createOrEditRequest.set({
        id: entity.id,
        isEnable: entity.isEnable,
        email: entity.email,
        avatarFileId: entity.avatarFileId,
        phoneNumber: entity.phoneNumber,
        roleIds: entity.roleIds,
        orgCodes: entity.orgCodes,
        username: entity.username,
        // password: entity.password,
      } as Partial<EditUserRequest>);
    } else {
      this.createOrEditRequest.set({
        email: undefined,
        orgCodes: [],
        phoneNumber: undefined,
        roleIds: [],
        claims: [],
        avatarFileId: undefined,
        isEnable: true,
        username: undefined,
      });
    }
  }
  id = signal<string>(undefined);
  resetUserPasswordRequest = {
    userId: undefined,
    password: undefined,
  };
  isChecked = signal(false);
  isVisible = signal(false);
  createOrEditRequest = signal<{ [key in keyof (Partial<EditUserRequest> | Partial<CreateUserRequest>)]: any }>({});
  schema: SFSchema = this.buildSchema();
  ui: SFUISchema = {
    "*": {
      spanLabelFixed: 100,
      grid: { span: 12 },
      class: "text-left",
    },
  };

  private buildSchema(): SFSchema {
    const t = this.I18N.Identity.user;
    return {
      properties: {
        username: {
          type: "string",
          title: t.schemaUsername,
          readOnly: this.isChecked,
          ui: {
            placeholder: t.schemaUsernamePlaceholder,
          },
        },
        password: {
          type: "string",
          title: t.schemaPassword,
          ui: {
            type: "password",
            placeholder: t.schemaPasswordPlaceholder,
            visibleIf: {
              password: () => this.id() == undefined,
            },
          },
        },
        phoneNumber: { type: "string", title: t.schemaPhone, format: "mobile", ui: { placeholder: t.schemaPhonePlaceholder } },
        email: { type: "string", title: t.schemaEmail, format: "email", ui: { placeholder: t.schemaEmailPlaceholder } },
        isEnable: { type: "boolean", title: t.schemaIsEnable },
        orgCodes: {
          type: "string",
          title: t.schemaOrgCodes,
          ui: {
            widget: "org-tree-select",
            multiple: true,
            checkable: true,
            orgType: [OrgTypeEnum.Default],
            filter: x => true,
          } as OrgTreeSelectWidgetSchema,
        },
        avatarFileId: {
          type: "string",
          title: t.schemaAvatar,
          ui: {
            widget: "geex-upload",
            valueEmitType: "id",
            limitFileCount: 1,
            listType: "picture-card",
          } as GeexUploadWidgetSchema,
        } as SFSchema,
        roleIds: {
          type: "number",
          title: t.schemaRoles,
          ui: {
            widget: "role-transfer",
          } as SFSelectWidgetSchema,
        },
      } as unknown as { [key in keyof EditUserRequest]: SFSchema },
      required: ["password", "roleIds", "username"],
    };
  }

  close(): void {
    if (this.paramsForm.dirty) {
      this.nzModalSrv.confirm({
        nzTitle: this.I18N.Identity.common.unsavedLeaveConfirm,
        nzOnOk: () => {
          this.location.back();
        },
      });
    } else {
      this.location.back();
    }
  }

  async submit(value: { [key in keyof Partial<CreateUserRequest>]: any }): Promise<void> {
    if (!!!this.id()) {
      let res: any = await this.apollo
        .mutate({
          mutation: createUser,
          variables: {
            request: {
              email: value.email,
              orgCodes: value.orgCodes,
              username: value.username,
              phoneNumber: value.phoneNumber,
              password: value.password,
              roleIds: value.roleIds,
              claims: [],
              isEnable: value.isEnable,
              avatarFileId: value.avatarFileId.firstOrDefault(),
            },
            // severity: value.severity,
          },
        })
        .firstValuePromise();
      if (res.data.createUser) {
        this.msgSrv.success(this.I18N.Identity.user.createSuccess);
        await this.router.navigate(["/identity/user"], { replaceUrl: true, forceReload: true });
      }
    } else {
      let res = await this.apollo
        .mutate({
          mutation: editUser,
          variables: {
            request: {
              id: this.id(),
              nickname: value.nickname ?? value.username,
              email: value.email,
              orgCodes: value.orgCodes,
              phoneNumber: value.phoneNumber,
              roleIds: value.roleIds,
              username: value.username,
              claims: [],
              isEnable: value.isEnable,
              avatarFileId: value.avatarFileId.firstOrDefault(),
            },
          },
        })
        .firstValuePromise();
      if (res.data.editUser) {
        this.msgSrv.success(this.I18N.Identity.user.updateSuccess);
        await this.router.navigate(["/identity/user"], { replaceUrl: true, forceReload: true });
      }
    }
  }
  showModal() {
    this.isVisible.set(true);
  }

  async handleOk() {
    var req = this.resetUserPasswordRequest;
    const res = await this.apollo
      .mutate({
        mutation: resetUserPassword,
        variables: {
          request: { userId: req.userId, password: SparkMD5.hash(req.password) || undefined } as ResetUserPasswordRequest,
        },
      })
      .firstValuePromise();
    this.resetUserPasswordRequest.password = undefined;
    this.isVisible.set(false);
    setTimeout(async () => {
      if (res.data.resetUserPassword.id) {
        this.msgSrv.success(this.I18N.Identity.user.resetSuccess);
      }
      else {
        this.msgSrv.error(this.I18N.Identity.user.resetFailed);
      }
    }, 100);
  }

  handleCancel(): void {
    this.isVisible.set(false);
  }
}


