import { Component, Injector, Input, OnInit, ViewChild } from "@angular/core";
import { SFComponent, SFSchema, SFSelectWidgetSchema, SFUISchema } from "@delon/form";
import { ModalHelper, _HttpClient } from "@delon/theme";

import { BusinessComponentBase } from "../../../../shared/components/business.component.base";
import {
  EditUserRequestInput,
  EditUserGql,
  CreateUserGql,
  CreateUserRequestInput,
  OrgTypeEnum,
  ResetUserPasswordGql,
  ResetUserPasswordRequestInput,
  UserByIdGql,
  UserByIdQuery,
  UserByIdQueryVariables,
} from "../../../../shared/graphql/.generated/type";
import { EditMode } from "../../../../shared/types/common";
import { GeexUploadWidgetSchema } from "../../../../shared/widgets/geex-upload.widget";
import { OrgTreeSelectWidgetSchema } from "../../../../shared/widgets/org-tree-select.widget";

@Component({
  selector: "app-user-edit",
  templateUrl: "./edit.component.html",
})
export class UserEditComponent extends BusinessComponentBase {
  mode: EditMode;
  id: string;
  data: ResetUserPasswordRequestInput = {
    userId: undefined,
    password: undefined,
  };
  isChecked = false;
  isVisible = false;
  defaultData: { [key in keyof (Partial<EditUserRequestInput> | Partial<CreateUserRequestInput>)]: any };
  @ViewChild("sf")
  readonly sf!: SFComponent;
  schema: SFSchema = {
    properties: {
      username: {
        type: "string",
        title: "用户名",
        readOnly: this.isChecked,
        ui: {
          placeholder: "请输入用户名",
        },
      },
      password: {
        type: "string",
        title: "密码",
        ui: {
          type: "password",
          placeholder: "设置密码",
          visibleIf: {
            password: () => this.mode == "create",
          },
        },
      },
      phoneNumber: { type: "string", title: "手机号", format: "mobile", ui: { placeholder: "请输入手机号" } },
      email: { type: "string", title: "邮箱", format: "email", ui: { placeholder: "邮箱地址" } },
      isEnable: { type: "boolean", title: "是否激活" },
      orgCodes: {
        type: "string",
        title: "组织关系",
        ui: {
          widget: "org-tree-select",
          multiple: true,
          orgType: [OrgTypeEnum.Default],
          filter: x => true,
        } as OrgTreeSelectWidgetSchema,
      },
      avatarFileId: {
        type: "string",
        title: "头像",
        ui: {
          widget: "geex-upload",
          valueEmitType: "id",
          limitFileCount: 1,
          listType: "picture-card",
        } as GeexUploadWidgetSchema,
      } as SFSchema,
      roleIds: {
        type: "number",
        title: "角色",
        ui: {
          widget: "role-transfer",
        } as SFSelectWidgetSchema,
      },
    } as unknown as { [key in keyof EditUserRequestInput]: SFSchema },
    required: ["password", "roleIds", "username" /*'userType', 'severity'*/],
  };
  ui: SFUISchema = {
    "*": {
      spanLabelFixed: 100,
      grid: { span: 12 },
      class: "text-left",
    },
  };

  constructor(injector: Injector, private mh: ModalHelper) {
    super(injector);
  }

  close(): void {
    this.nzModalSrv.confirm({
      nzTitle: "当前页面内容未保存，确定离开？",
      nzOnOk: () => {
        this.router.navigate(["/identity/user"]);
      },
    });
  }

  async prepare(params: any) {
    this.id = params.id;
    this.mode = params.id == undefined ? "create" : "edit";
    if (params.id) {
      this.data.userId = params.id;
      let res = await this.apollo
        .query<UserByIdQuery, UserByIdQueryVariables>({
          query: UserByIdGql,
          variables: {
            id: params.id,
          },
        })
        .toPromise();
      this.schema.properties.username.readOnly = true;
      let entity = res.data.users.items[0];
      this.defaultData = {
        id: entity.id,
        isEnable: entity.isEnable,
        email: entity.email,
        avatarFileId: entity.avatarFileId,
        phoneNumber: entity.phoneNumber,
        roleIds: entity.roleIds,
        orgCodes: entity.orgCodes,
        username: entity.username,
        // password: entity.password,
      } as Partial<EditUserRequestInput>;
    } else {
      this.defaultData = {
        email: undefined,
        orgCodes: [],
        phoneNumber: undefined,
        roleIds: [],
        claims: [],
        avatarFileId: undefined,
        isEnable: true,
        username: undefined,
      };
    }
  }

  async submit(value: { [key in keyof Partial<CreateUserRequestInput>]: any }): Promise<void> {
    if (this.mode == "create") {
      let res = await this.apollo
        .mutate({
          mutation: CreateUserGql,
          variables: {
            input: {
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
        .toPromise();
      if (res.data.createUser) {
        this.msgSrv.success("创建成功");
        await this.router.navigate(["/identity/user"], { relativeTo: this.route, forceReload: true });
      }
    } else {
      let res = await this.apollo
        .mutate({
          mutation: EditUserGql,
          variables: {
            input: {
              id: this.id,
              email: value.email,
              orgCodes: value.orgCodes,
              phoneNumber: value.phoneNumber,
              // password: value.password,
              roleIds: value.roleIds,
              username: value.username,
              claims: [],
              isEnable: value.isEnable,
              avatarFileId: value.avatarFileId.firstOrDefault(),
            },
            // name: value.name,
            // userType: value.userType,
            // severity: value.severity,
          },
        })
        .toPromise();
      if (res.data.editUser) {
        this.msgSrv.success("修改成功");
        await this.router.navigate(["/identity/user"], { relativeTo: this.route, forceReload: true });
      }
    }
  }
  showModal() {
    this.isVisible = true;
  }

  async handleOk() {
    await this.apollo
      .mutate({
        mutation: ResetUserPasswordGql,
        variables: {
          input: {
            userId: this.data.userId,
            password: this.data.password,
          },
        },
      })
      .toPromise();
    if (this.data.password) {
      this.msgSrv.success("重置成功");
      await this.router.navigate(["/identity/user"], { relativeTo: this.route });
    }
    this.data.password = undefined;
    this.isVisible = false;
  }

  handleCancel(): void {
    this.isVisible = false;
  }
}
