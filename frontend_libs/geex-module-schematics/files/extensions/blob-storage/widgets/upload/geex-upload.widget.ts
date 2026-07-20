

import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ControlUIWidget, toBool } from "@delon/form";
import { DelonFormModule } from "@delon/form";
import { SFUploadWidgetSchema } from "@delon/form/widgets/upload";
import { deepGet } from "@delon/util/other";
import { Apollo } from "apollo-angular";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzUploadChangeParam, NzUploadFile, NzUploadXHRArgs } from "ng-zorro-antd/upload";
import { NzUploadModule } from "ng-zorro-antd/upload";
import { NzButtonModule } from "ng-zorro-antd/button";
import { NzIconModule } from "ng-zorro-antd/icon";
import { from, Observable, Subscription } from "rxjs";
import { switchMap } from "rxjs/operators";

import { GEEX_I18N, geex } from "@geexcode/geex-angular";

export type GeexUploadWidgetSchema = SFUploadWidgetSchema & {
  valueEmitType?: "id" | "file";
  idReName: string;
  customFileSize: number;
  deleteRemoteFile?: Observable<boolean>;
  storageType?: string;
};

@Component({
  selector: "sf-widget-upload",
  template: `<sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-upload
      [nzType]="$any(ui.type)"
      [(nzFileList)]="fileList"
      [nzDisabled]="ui.readOnly"
      [nzAction]="ui.action"
      [nzDirectory]="ui.directory"
      [nzOpenFileDialogOnClick]="ui.openFileDialogOnClick"
      [nzAccept]="ui.accept"
      [nzLimit]="ui.limit"
      [nzFilter]="$any(ui.filter)"
      [nzSize]="ui.fileSize"
      [nzFileType]="ui.fileType"
      [nzHeaders]="ui.headers"
      [nzData]="ui.data"
      [nzListType]="$any(ui.listType)"
      [nzMultiple]="ui.multiple"
      [nzName]="$any(ui.name)"
      [nzShowUploadList]="ui.showUploadList"
      [nzWithCredentials]="ui.withCredentials"
      [nzBeforeUpload]="$any(ui.beforeUpload)"
      [nzCustomRequest]="$any(ui.customRequest)"
      [nzRemove]="ui.remove || handleRemove"
      [nzPreview]="handlePreview"
      [nzPreviewFile]="ui.previewFile"
      [nzDownload]="ui.download"
      [nzTransformFile]="ui.transformFile"
      (nzChange)="change($event)"
      [nzShowButton]="fileList.length < (ui.limitFileCount ?? 999)"
    >
      @switch (btnType) {
        @case ("plus") {
          <i nz-icon nzType="plus"></i>
          <div class="ant-upload-text" [innerHTML]="ui.text"></div>
        }
        @case ("drag") {
          <p class="ant-upload-drag-icon"><i nz-icon nzType="uinbox"></i></p>
          <p class="ant-upload-text" [innerHTML]="ui.text"></p>
          <p class="ant-upload-hint" [innerHTML]="ui.hint"></p>
        }
        @default {
          <button type="button" nz-button [disabled]="disabled">
            <i nz-icon nzType="upload"></i>
            <span [innerHTML]="ui.text"></span>
          </button>
        }
      }
    </nz-upload>
  </sf-item-wrap>`,
  standalone: true,
  imports: [DelonFormModule, NzUploadModule, NzButtonModule, NzIconModule],
})
export class GeexUploadWidget extends ControlUIWidget<GeexUploadWidgetSchema> {
  static readonly KEY = "geex-upload";

  fileList: NzUploadFile[] = [];
  btnType = "";

  ngOnInit(): void {
    const {
      type,
      text,
      hint,
      action,
      accept,
      limit,
      filter,
      fileSize,
      customFileSize,
      deleteRemoteFile,
      fileType,
      listType,
      multiple,
      name,
      showUploadList,
      withCredentials,
      beforeUpload,
      customRequest,
      directory,
      openFileDialogOnClick,
      limitFileCount,
      valueEmitType,
      idReName,
      storageType,
    } = this.ui as GeexUploadWidgetSchema;

    const blobStorage = geex.blobStorage;
    const defaultStorageType = blobStorage.defaultStorageType;
    const createDocument = blobStorage.createDocument;
    const I18N = this.injector.get(GEEX_I18N) as any;

    const res: GeexUploadWidgetSchema = {
      type: type || "select",
      text: text || I18N.BlobStorage.uploadClick,
      action: action || "",
      accept: accept || "",
      directory: toBool(directory, false),
      openFileDialogOnClick: toBool(openFileDialogOnClick, true),
      limit: limit == null ? 0 : +limit,
      filter: filter == null ? [] : filter,
      fileSize: fileSize == null ? 0 : +fileSize,
      customFileSize: customFileSize == null ? 0 : +customFileSize,
      deleteRemoteFile,
      fileType: fileType || "",
      listType: listType || "text",
      multiple: toBool(multiple, false),
      name: name || "file",
      showUploadList: showUploadList == null ? true : showUploadList,
      withCredentials: toBool(withCredentials, false),
      spanLabel: this.ui.spanLabel || 5,
      spanControl: this.ui.spanControl || 19,
      beforeUpload:
        typeof beforeUpload === "function"
          ? beforeUpload
          : args => {
              if (customFileSize == 0 || customFileSize == null) {
                return true;
              }
              if ((args.size ?? 0) / 1024 > customFileSize) {
                this.injector.get(NzMessageService).error(I18N.BlobStorage.fileSizeExceeded.replace("{size}", String(customFileSize)));
                return false;
              }
              return true;
            },
      customRequest:
        typeof customRequest === "function"
          ? customRequest
          : (args: NzUploadXHRArgs) => {
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
                    return this.injector
                      .get(Apollo)
                      .mutate({
                        mutation: createDocument,
                        variables: {
                          request: {
                            file: args.file as any,
                            md5,
                            storageType: storageType ?? defaultStorageType,
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
            },
      urlReName: "url",
      limitFileCount: limitFileCount || 999,
      valueEmitType,
      idReName: idReName || "id",
      storageType: storageType ?? defaultStorageType,
      download: file => {
        window.open(file.url, "_blank");
      },
    };
    if (res.listType === "picture-card") {
      this.btnType = "plus";
    }
    if (res.type === "drag") {
      res.listType = undefined;
      this.btnType = "drag";
      res.text = text || I18N.BlobStorage.uploadDragText;
      res.hint = hint || I18N.BlobStorage.uploadDragHint;
    }
    this.ui = res;
    this.defaultStorageType = defaultStorageType;
  }

  private defaultStorageType = "";

  change(args: NzUploadChangeParam): void {
    if (this.ui.change) this.ui.change(args);
    if (args.type !== "success") return;
    this._setValue(args.fileList);
  }

  override reset(value: Array<string | NzUploadFile>): void {
    if (value == null) {
      this.setValue([]);
      return;
    }
    let { fileList } = this.ui;
    if (fileList?.any()) {
      throw new Error("不支持通过filelist传值, 请根据valueEmitType直接设置formdata为blobObjectId或者file对象");
    }
    const blobStorage = geex.blobStorage;
    if (this.ui.valueEmitType == "id") {
      blobStorage
        .list({
          includeDetail: true,
          filter: {
            id: {
              in: value as string[],
            },
          },
        })
        .then(data => {
          const blobs = (data as { blobObjects: { items: Array<Record<string, unknown>> } }).blobObjects.items;
          fileList = blobs.map(y => ({
            name: String(y.fileName),
            uid: String(y.id),
            size: Number(y.fileSize),
            url: String(y.url),
            status: "done" as const,
            linkProps: {
              download: String(y.url),
            },
          }));
          this.fileList = fileList ?? [];
          this.setValue(this.schema.transformIn ? this.schema.transformIn(value) : value);
        });
    } else {
      fileList = value as NzUploadFile[];
      this.fileList = fileList;
      this.formProperty._value = this.pureValue(fileList);
      this.formProperty.updateValueAndValidity({ onlySelf: true, emitValueEvent: false, emitValidator: false });
      this.detectChanges(true);
    }
  }

  private _getValue(file: NzUploadFile): NzSafeAny {
    if (this.ui.valueEmitType == "id") {
      return file.uid;
    }
    return deepGet(file.response, this.ui.resReName, file.response);
  }

  private pureValue(fileList: NzUploadFile[]): any[] {
    fileList
      .filter(file => !file.url)
      .forEach(file => {
        file.url = deepGet(file.response, this.ui.urlReName);
        file.id = file.uid = deepGet(file.response, this.ui.idReName);
      });
    const res = fileList.filter(w => w.status === "done").map(file => this._getValue(file));
    if (this.schema.transformOut) {
      return this.schema.transformOut(res);
    }
    return res;
  }

  private _setValue(fileList: NzUploadFile[]): void {
    this.setValue(this.pureValue(fileList));
  }

  handleRemove = (file: NzUploadFile): Observable<boolean> =>
    from(
      (async () => {
        this.fileList = this.fileList.filter(f => f.uid !== file.uid);
        this._setValue(this.fileList);
        let isDelete = true;
        if (this.ui.deleteRemoteFile) {
          isDelete = (await this.ui.deleteRemoteFile.firstValuePromise()) ?? true;
        }
        if (isDelete && file.uid) {
          await geex.blobStorage.delete({
            request: {
              ids: [file.uid],
              storageType: this.ui.storageType ?? this.defaultStorageType,
            },
          });
        }
        return true;
      })(),
    );

  handlePreview = (file: NzUploadFile) => {
    if (this.ui.preview) {
      this.ui.preview(file);
      return;
    }
    const _url = file.thumbUrl || file.url;
    if (!_url) {
      return;
    }
    this.injector.get<NzModalService>(NzModalService).create({
      nzContent: `<img src="${_url}" class="img-fluid" />`,
      nzFooter: null,
    });
  };
}
