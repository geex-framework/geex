import { Component, inject } from "@angular/core";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { Apollo } from "apollo-angular";
import { NzMessageService } from "ng-zorro-antd/message";
import { BlobStorageType } from "@/gql";
import { SharedModule } from "@/shared/shared.module";
import { createBlobObject } from "../../graphql/operations.gql";

@Component({
  selector: "app-blob-storage-edit",
  standalone: true,
  imports: [SharedModule],
  templateUrl: "./edit.page.html",
})
export class BlobStorageEditPage {
  private readonly apollo = inject(Apollo);
  private readonly message = inject(NzMessageService);
  readonly router = inject(Router);
  readonly storageTypes = Object.values(BlobStorageType);
  readonly form = new FormGroup({
    file: new FormControl<File | null>(null, Validators.required),
    storageType: new FormControl<BlobStorageType>(BlobStorageType.FileSystem, { nonNullable: true }),
  });

  selectFile(event: Event): void {
    this.form.controls.file.setValue((event.target as HTMLInputElement).files?.item(0) ?? null);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || !this.form.controls.file.value) return;
    await this.apollo.mutate({
      mutation: createBlobObject,
      variables: { request: { file: this.form.controls.file.value as never, storageType: this.form.controls.storageType.value } },
      context: { useMultipart: true },
    }).firstValuePromise();
    this.message.success("File uploaded");
    await this.router.navigate(["/blob-storage"]);
  }
}
