import { Component, signal, computed, inject } from "@angular/core";
import { STColumn } from "@delon/abc/st";
import { SharedModule } from "@/shared/shared.module";
import { RoutedListComponent } from "@geexcode/geex-angular";
import { RouteParamsMappings } from "@geexcode/geex-angular";
import type { IOrgFilterInput } from "@/gql";
import { orgs, deleteOrg } from "../../graphql/org.operations.gql";
import type { OrgBrief } from "../../graphql/org.operations.gql";
import { ModalHelper, _HttpClient } from "@delon/theme";
import { AppPermission } from "@/gql";
import { OrgEditModalComponent } from "@/modules/identity/components/modals/org-edit-modal.component";

export type OrgListParams = {
  pi: number;
  ps: number;
  name: string;
};

@Component({
  selector: "app-org-list",
  templateUrl: "./list.page.html",
  standalone: true,
  imports: [SharedModule],
})
export class OrgListComponent extends RoutedListComponent<OrgListParams, OrgBrief> {
  private modalSrv = inject(ModalHelper);

  override columns = [
    { title: "", width: 30, type: "checkbox", index: "checked", fixed: "left", className: ["text-center"] },
    { title: "编码", index: "code", className: "text-center" },
    { title: "名称", index: "name", className: "text-center" },
    { title: "父组织", index: "parentOrgCode", className: "text-center" },
    {
      title: "操作",
      buttons: [
        {
          icon: "edit",
          text: "编辑",
          click: item => this.edit(item),
          acl: AppPermission.identity_mutation_editOrg,
        },
        {
          icon: "delete",
          text: "删除",
          click: item => this.delete(item),
          acl: AppPermission.identity_mutation_editOrg,
        },
      ],
      className: "text-center",
    },
  ] as STColumn<OrgBrief>[];

  override routeParamsMappings: RouteParamsMappings<OrgListParams> = {
    pi: { position: "queryParams", default: 1 },
    ps: { position: "queryParams", default: 10 },
    name: { position: "queryParams", default: "" },
  };

  override async onRouted(params: OrgListParams) {
    let filter: IOrgFilterInput = undefined;
    if (params.name) {
      filter = { name: { contains: params.name } } as IOrgFilterInput;
    }
    const res: any = await this.apollo
      .query({
        query: orgs,
        variables: { filter },
        fetchPolicy: "no-cache",
      })
      .firstValuePromise();
    const items = res.data.orgs.items ?? [];
    this.data.set(items);
    this.total.set(items.length);
  }

  add() {
    this.createOrg();
  }

  private async createOrg(parentCode?: string) {
    const org = { name: undefined, code: undefined, parentCode };
    const res = await this.modalSrv.createStatic(OrgEditModalComponent, { org }, { size: 500, exact: true }).lastValuePromise();
    if (res) { this.refresh(); }
  }

  private async edit(item: OrgBrief) {
    const res = await this.modalSrv.createStatic(OrgEditModalComponent, { org: item }, { size: 500, exact: true }).lastValuePromise();
    if (res) { this.refresh(); }
  }

  private async delete(item: OrgBrief) {
    this.nzModalSrv.confirm({
      nzTitle: `确认删除组织“${item.name}”?`,
      nzOnOk: async () => {
        await this.apollo.mutate({ mutation: deleteOrg, variables: { ids: [item.id] }, refetchQueries: [orgs] }).firstValuePromise();
        this.refresh();
      },
    });
  }
}


