

import { Component, ContentChild, forwardRef, inject, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import { Apollo } from "apollo-angular";
import * as _ from "lodash-es";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";
import {
  NzUploadChangeParam,
  NzUploadFile,
  NzUploadListType,
  NzUploadTransformFileType,
  NzUploadType,
  NzUploadXHRArgs,
  UploadFilter,
} from "ng-zorro-antd/upload";
import { NzUploadModule } from "ng-zorro-antd/upload";
import { from, Observable, Subscription } from "rxjs";
import { switchMap } from "rxjs/operators";

import { geex } from "@geexcode/geex-angular";

@Component({
  selector: "geex-upload",
  templateUrl: "./geex-upload.component.html",
  standalone: true,
  imports: [CommonModule, NzUploadModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => GeexUploadComponent),
      multi: true,
    },
  ],
})
export class GeexUploadComponent implements ControlValueAccessor {
  private readonly apollo = inject(Apollo);
  private readonly messageService = inject(NzMessageService);
  private readonly modalService = inject(NzModalService);
  private readonly blobStorage = geex.blobStorage;

  @ContentChild("uploadButton") uploadButton: any;

  @Input() nzShowUploadList = true;
  @Input() nzDisabled = false;
  @Input() nzFileList: NzUploadFile[] = [];
  @Input() nzLimit = 1;
  @Input() nzSize = 0;
  @Input() nzFileType: string | undefined;
  @Input() nzFilter: UploadFilter[] = [];
  @Input() nzListType: NzUploadListType = "text";
  @Input() nzShowButton = true;
  @Input() nzOpenFileDialogOnClick = true;
  @Input() nzRemove?: (file: NzUploadFile) => boolean | Observable<boolean>;
  @Input() nzAccept: string | undefined;
  @Input() nzAction: string | undefined;
  @Input() nzDirectory = false;
  @Input() nzMultiple = false;
  @Input() nzName = "file";
  @Input() nzHeaders: {} | undefined;
  @Input() nzData?: {} | ((file: NzUploadFile) => {} | Observable<{}>);
  @Input() nzWithCredentials = false;
  @Input() nzPreviewFile?: (file: NzUploadFile) => Observable<string>;
  @Input() nzDownload?: (file: NzUploadFile) => void;
  @Input() nzTransformFile?: (file: NzUploadFile) => NzUploadTransformFileType;

  @Input() valueEmitType: "id" | "file" = "id";
  @Input() idReName: string = "id";
  @Input() urlReName: string = "url";
  @Input() resReName: string = "";
  @Input() customFileSize: number = 102400;
  @Input() deleteRemoteFile?: Observable<boolean>;
  @Input() storageType?: string;
  @Input() nzType?: NzUploadType;

  disabled = false;
  fileList: NzUploadFile[] = [];

  onChange = (_: any) => {};
  onTouched = () => {};

  writeValue(obj: any): void {
    if (obj == null) {
      this.fileList = [];
    } else if (this.valueEmitType === "id") {
      this.fetchFilesByIds(obj).then(fileList => {
        this.fileList = fileList;
      });
    } else {
      this.fileList = obj;
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  change(args: NzUploadChangeParam): void {
    if (args.type !== "success") return;
    this.fileList = args.fileList;
    this.onChange(this.pureValue(this.fileList));
  }

  private pureValue(fileList: NzUploadFile[]): any[] {
    fileList
      .filter(file => !file.url)
      .forEach(file => {
        file.url = _.get(file.response, this.urlReName);
        file.id = file.uid = _.get(file.response, this.idReName);
      });
    const res = fileList.filter(w => w.status === "done").map(file => this._getValue(file));
    if (this.nzMultiple) {
      return res;
    }
    return res.elementAtOrDefault(0);
  }

  private _getValue(file: NzUploadFile): any {
    if (this.valueEmitType === "id") {
      return file.uid;
    }
    return _.get(file.response, this.resReName, file.response);
  }

  handleRemove = (file: NzUploadFile): boolean | Observable<boolean> => {
    this.fileList = this.fileList.filter(f => f.uid !== file.uid);
    this.onChange(this.pureValue(this.fileList));
    if (this.deleteRemoteFile) {
      return this.deleteRemoteFile.pipe(
        switchMap(isDelete => {
          if (isDelete) {
            const ids = [file.uid];
            return geex.blobStorage
              .delete({
                request: {
                  ids,
                  storageType: this.storageType ?? this.blobStorage.defaultStorageType,
                },
              })
              .then(() => true);
          }
          return Promise.resolve(true);
        }),
      );
    }
    return true;
  };

  handlePreview = (file: NzUploadFile): void => {
    const _url = file.thumbUrl || file.url;
    if (!_url) {
      return;
    }
    this.modalService.create({
      nzContent: `<img src="${_url}" class="img-fluid" />`,
      nzFooter: null,
    });
  };

  beforeUpload = (file: NzUploadFile): boolean => {
    if (this.customFileSize === 0 || this.customFileSize == null) {
      return true;
    }
    if ((file.size ?? 0) / 1024 > this.customFileSize) {
      this.messageService.error(`上传文件超过了设置的文件大小: ${this.customFileSize}KB`);
      return false;
    }
    return true;
  };

  uploadGeexBlobObject = (args: NzUploadXHRArgs): Subscription => {
    const createDocument = this.blobStorage.createDocument;
    if (!createDocument) {
      return Subscription.EMPTY;
    }
    const postFile = args.postFile;
    if (!(postFile instanceof Blob)) {
      return Subscription.EMPTY;
    }
    return from(postFile.slice().computeChecksumMd5())
      .pipe(
        switchMap(md5 => {
          args.onProgress?.({ percent: 50 }, args.file);
          return this.apollo
            .mutate({
              mutation: createDocument,
              variables: {
                request: {
                  file: args.file as any,
                  md5,
                  storageType: this.storageType ?? this.blobStorage.defaultStorageType,
                },
              },
              context: {
                useMultipart: true,
              },
            })
            .firstValuePromise();
        }),
      )
      .subscribe(x => {
        args.onProgress?.({ percent: 100 }, args.file);
        args.onSuccess?.((x?.data as { createBlobObject: unknown } | undefined)?.createBlobObject, args.file, null);
      });
  };

  private async fetchFilesByIds(ids: string[] | string): Promise<NzUploadFile[]> {
    const idList = Array.isArray(ids) ? ids : [ids];
    const data = await this.blobStorage.list({
      includeDetail: true,
      filter: {
        id: {
          in: idList,
        },
      },
    });

    const blobs = (data as { blobObjects: { items: Array<Record<string, unknown>> } } | undefined)?.blobObjects;
    if (!blobs) {
      return [];
    }
    return blobs.items.map(
      y =>
        ({
          name: String(y.fileName),
          uid: String(y.id),
          size: Number(y.fileSize),
          url: String(y.url),
          status: "done",
          linkProps: {
            download: String(y.url),
          },
        }) satisfies NzUploadFile,
    );
  }
}
