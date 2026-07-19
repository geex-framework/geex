import { Component, inject, OnInit, signal } from "@angular/core";
import { Router } from "@angular/router";
import type { STColumn } from "@delon/abc/st";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { SharedModule } from "@/shared/shared.module";
import { blobObjects, deleteBlobObject, type BlobObjectBrief } from "../../graphql/operations.gql";

@Component({
  selector: "app-blob-storage-list",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./list.page.html",
})
export class BlobStorageListPage implements OnInit {
  private readonly apollo = inject(Apollo);
  private readonly message = inject(NzMessageService);
  private readonly router = inject(Router);
  readonly loading = signal(false);
  readonly data = signal<BlobObjectBrief[]>([]);
  readonly total = signal(0);
  filterText = "";
  pageIndex = 1;
  pageSize = 10;
  readonly columns: STColumn<BlobObjectBrief>[] = [
    { title: "File name", index: "fileName" },
    { title: "Size", index: "fileSize", type: "number" },
    { title: "MIME type", index: "mimeType" },
    { title: "Storage", index: "storageType" },
    { title: "Created", index: "createdOn", type: "date" },
    { title: "Actions", buttons: [
      { text: "Open", click: item => item.url && window.open(item.url, "_blank") },
      { text: "Delete", type: "del", click: item => this.delete(item) },
    ] },
  ];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.apollo.query({
        query: blobObjects,
        variables: {
          filter: this.filterText ? { fileName: { contains: this.filterText } } : undefined,
          skip: (this.pageIndex - 1) * this.pageSize,
          take: this.pageSize,
        },
        fetchPolicy: "no-cache",
      }).firstValuePromise();
      this.data.set((result.data.blobObjects?.items ?? []).filter((item): item is BlobObjectBrief => item != null));
      this.total.set(result.data.blobObjects?.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }

  add(): void { void this.router.navigate(["/blob-storage/edit"]); }

  async delete(item: BlobObjectBrief): Promise<void> {
    await this.apollo.mutate({
      mutation: deleteBlobObject,
      variables: { request: { ids: [item.id], storageType: item.storageType } },
    }).firstValuePromise();
    this.message.success("File deleted");
    await this.load();
  }
}
