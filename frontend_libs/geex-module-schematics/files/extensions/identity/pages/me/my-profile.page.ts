import { Component, Injector, Signal, computed, inject, signal } from "@angular/core";
import { FormGroup } from "@angular/forms";
import { SettingsService } from "@delon/theme";
import { ArrayService } from "@delon/util";
import { NzFormatEmitEvent, NzTreeNode } from "ng-zorro-antd/tree";

import { RoutedComponent } from "@geexcode/geex-angular";
import { SharedModule } from "@/shared/shared.module";
import type { ChangePasswordRequest, GeneratePersonalAccessTokenRequest } from "@/gql";
import { changePassword } from "../../graphql/user.operations.gql";
import type { User, Org } from "@geexcode/geex-extensions-identity";
import { generatePersonalAccessToken } from "../../graphql/me.operations.gql";

@Component({
  selector: "app-my-profile",
  templateUrl: "./my-profile.page.html",
  standalone: true,
  imports: [SharedModule],
})
export class MyProfilePage extends RoutedComponent<{}> {
  override routeParamsMappings: {} = {};
  override onRouted(params: {}): void | Promise<void> {
    return;
  }
  userData$: Signal<User>;
  orgs$: Signal<Org[]>;
  validateForm!: FormGroup;
  id: string;
  pageNo = 0;
  pageSize = 10;
  isVisible = false;
  confirmPassword: string;
  data: ChangePasswordRequest = {
    originPassword: undefined,
    newPassword: undefined,
  };
  nodes$: Signal<NzTreeNode[]>;
  activatedNode$ = signal<NzTreeNode>(undefined);
  
  // Personal Access Token 相关 - 使用 signal
  isTokenModalVisible$ = signal<boolean>(false);
  generatedToken$ = signal<string>('');
  tokenExpireDays$ = signal<number>(30); // UI 显示用天数
  get user(): User {
    return this.settings.user;
  }
  private settings = inject(SettingsService);
  private arrService = inject(ArrayService);
  constructor() {
    super();
    this.orgs$ = geex.identity.orgs;
    this.userData$ = geex.auth.user;
    this.nodes$ = computed(() => {
      let userData = this.userData$();
      if (userData == undefined) {
        return [];
      }
      let orgs = userData.orgs.concat(userData.orgs.selectMany(x => x.allParentOrgs as any)).distinctBy(x => x.code);
      let data = orgs.map(x => ({
        code: x.code,
        name: x.name,
        expanded: x.code.lastIndexOf(".") == -1,
        pCode: x.code.substring(0, x.code.lastIndexOf(".")),
      }));

      // 此处可能有多个根节点, 不能使用arrToTreeNode
      return this.arrService.arrToTreeNode(data, {
        parentIdMapName: "pCode",
        idMapName: "code",
        titleMapName: "name",
      });
    });
  }

  activeNode(data: NzFormatEmitEvent): void {
    if (data.node.isSelected) {
      this.pageNo = 0;
      this.pageSize = 10;
      this.activatedNode$.set(data.node!);
    } else {
      this.activatedNode$.set(undefined);
    }
  }

  showModal() {
    this.isVisible = true;
  }

  async handleOk() {
    await this.apollo
      .mutate({
        mutation: changePassword,
        variables: {
          request: {
            originPassword: this.data.originPassword,
            newPassword: this.data.newPassword,
          },
        },
      })
      .firstValuePromise();
    this.msgSrv.success("修改成功");
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;

    this.isVisible = false;
  }

  handleCancel(): void {
    this.confirmPassword = undefined;
    this.data.originPassword = undefined;
    this.data.newPassword = undefined;
    this.isVisible = false;
  }

  // 显示生成 Token 的模态框
  showTokenModal() {
    this.isTokenModalVisible$.set(true);
    this.generatedToken$.set('');
  }

  // 生成 Personal Access Token
  async handleGenerateToken() {
    try {
      const result = await this.apollo
        .mutate({
          mutation: generatePersonalAccessToken,
          variables: {
            req: {
              expireInSeconds: this.tokenExpireDays$() * 24 * 60 * 60, // 将天数转换为秒
            },
          },
        })
        .firstValuePromise();
      
      this.generatedToken$.set(result?.data?.generatePersonalAccessToken?.token || '');
      this.msgSrv.success("Token 生成成功，请妥善保管！");
    } catch (error) {
      this.msgSrv.error("Token 生成失败");
      console.error(error);
    }
  }

  // 关闭 Token 模态框
  handleTokenModalCancel(): void {
    this.isTokenModalVisible$.set(false);
    this.generatedToken$.set('');
    this.tokenExpireDays$.set(30);
  }

  // 复制 Token 到剪贴板
  copyToken() {
    const token = this.generatedToken$();
    if (token) {
      navigator.clipboard.writeText(token);
      this.msgSrv.success("Token 已复制到剪贴板");
    }
  }
}


