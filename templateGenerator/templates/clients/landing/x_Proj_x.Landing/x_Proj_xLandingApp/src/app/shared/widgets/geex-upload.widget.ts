/* eslint-disable prettier/prettier */
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Injector } from "@angular/core";
import { ControlUIWidget, ControlWidget, getData, SFComponent, SFItemComponent, SFUploadWidgetSchema, SFValue, toBool } from "@delon/form";
import { deepGet } from "@delon/util/other";
import { Apollo } from "apollo-angular";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { NzMessageService } from "ng-zorro-antd/message";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzUploadFile, NzUploadChangeParam } from "ng-zorro-antd/upload";
import { from, of } from "rxjs";
import { map, switchMap } from "rxjs/operators";

import { deepProxy } from "../../../shared/extensions";
import {
  CreateBlobObjectGql,
  BlobStorageType,
  IBlobObject,
  Maybe,
  BlobObjectsQuery,
  BlobObjectsQueryVariables,
  BlobObjectsGql,
} from "../graphql/.generated/type";
declare type FormHooks = "change" | "blur" | "submit";
export declare type GeexUploadWidgetSchema = SFUploadWidgetSchema & { valueEmitType?: "id" | "file"; idReName: string };
@Component({
  selector: "sf-widget-upload",
  template: `<sf-item-wrap [id]="id" [schema]="schema" [ui]="ui" [showError]="showError" [error]="error" [showTitle]="schema.title">
    <nz-upload
      [nzType]="ui.type"
      [(nzFileList)]="fileList"
      [nzDisabled]="ui.readOnly"
      [nzAction]="ui.action"
      [nzDirectory]="ui.directory"
      [nzOpenFileDialogOnClick]="ui.openFileDialogOnClick"
      [nzAccept]="ui.accept"
      [nzLimit]="ui.limit"
      [nzFilter]="ui.filter"
      [nzSize]="ui.size"
      [nzFileType]="ui.fileType"
      [nzHeaders]="ui.headers"
      [nzData]="ui.data"
      [nzListType]="ui.listType"
      [nzMultiple]="ui.multiple"
      [nzName]="ui.name"
      [nzShowUploadList]="ui.showUploadList"
      [nzWithCredentials]="ui.withCredentials"
      [nzBeforeUpload]="ui.beforeUpload"
      [nzCustomRequest]="ui.customRequest"
      [nzRemove]="ui.remove || handleRemove"
      [nzPreview]="handlePreview"
      [nzPreviewFile]="ui.previewFile"
      [nzDownload]="ui.download"
      [nzTransformFile]="ui.transformFile"
      (nzChange)="change($event)"
      [nzShowButton]="fileList.length < ui.limitFileCount"
    >
      <ng-container [ngSwitch]="btnType">
        <ng-container *ngSwitchCase="'plus'">
          <i nz-icon nzType="plus"></i>
          <div class="ant-upload-text" [innerHTML]="ui.text"></div>
        </ng-container>
        <ng-container *ngSwitchCase="'drag'">
          <p class="ant-upload-drag-icon"><i nz-icon nzType="uinbox"></i></p>
          <p class="ant-upload-text" [innerHTML]="ui.text"></p>
          <p class="ant-upload-hint" [innerHTML]="ui.hint"></p>
        </ng-container>
        <ng-container *ngSwitchDefault>
          <button type="button" nz-button [disabled]="disabled">
            <i nz-icon nzType="upload"></i>
            <span [innerHTML]="ui.text"></span>
          </button>
        </ng-container>
      </ng-container>
    </nz-upload>
  </sf-item-wrap>`,
  host: {
    // "(click)": "click()"
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeexUploadWidget extends ControlUIWidget<GeexUploadWidgetSchema> {
  static readonly KEY = "geex-upload";
  fileList: NzUploadFile[] = [];
  btnType = "";
  /**
   *
   */
  constructor(cd: ChangeDetectorRef, injector: Injector, sfItemComp?: SFItemComponent, sfComp?: SFComponent) {
    super(cd, injector, sfItemComp, sfComp);
  }
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
      spanControl,
      spanLabel,
    } = this.ui as GeexUploadWidgetSchema;
    const res: GeexUploadWidgetSchema = {
      type: type || "select",
      text: text || "点击上传",
      action: action || "",
      accept: accept || "",
      directory: toBool(directory, false),
      openFileDialogOnClick: toBool(openFileDialogOnClick, true),
      limit: limit == null ? 0 : +limit,
      filter: filter == null ? [] : filter,
      fileSize: fileSize == null ? 0 : +fileSize,
      fileType: fileType || "",
      listType: listType || "text",
      multiple: toBool(multiple, false),
      name: name || "file",
      showUploadList: showUploadList == null ? true : showUploadList,
      withCredentials: toBool(withCredentials, false),
      spanLabel: this.ui.spanLabel || 5,
      spanControl: this.ui.spanControl || 19,
      // resReName: (resReName || "").split("."),
      // urlReName: (urlReName || "").split("."),
      beforeUpload: typeof beforeUpload === "function" ? beforeUpload : null,
      customRequest:
        typeof customRequest === "function"
          ? customRequest
          : args => {
              // console.log(args);
              // args.onProgress({ percent: 10 }, args.file);
              return from((args.postFile.slice() as Blob).computeChecksumMd5())
                .pipe(
                  switchMap(md5 => {
                    args.onProgress({ percent: 50 }, args.file);
                    return this.injector
                      .get(Apollo)
                      .mutate({
                        mutation: CreateBlobObjectGql,
                        variables: {
                          input: {
                            file: args.file as any,
                            md5: md5,
                            storageType: storageType ?? BlobStorageType.Db,
                          },
                        },
                        context: {
                          useMultipart: true,
                        },
                      })
                      .toPromise();
                  }),
                )
                .subscribe(x => {
                  args.onProgress({ percent: 100 }, args.file);
                  args.onSuccess(x.data.createBlobObject, args.file, null);
                });
            },
      urlReName: "url",
      limitFileCount: limitFileCount || 999,
      valueEmitType: valueEmitType,
      idReName: idReName || "id",
      download: file => {
        window.open(file.url, null, null);
      },
    };
    if (res.listType === "picture-card") {
      this.btnType = "plus";
    }
    if (res.type === "drag") {
      res.listType = null;
      this.btnType = "drag";
      res.text = text || `单击或拖动文件到该区域上传`;
      res.hint = hint || `支持单个或批量，严禁上传公司数据或其他安全文件`;
    }
    this.ui = res;
  }
  change(args: NzUploadChangeParam): void {
    if (this.ui.change) this.ui.change(args);
    if (args.type !== "success") return;
    this._setValue(args.fileList);
  }
  reset(value: Array<string | NzUploadFile>): void {
    if (value == null) {
      this.setValue([]);
      return;
    }
    // console.log(value);
    let { fileList } = this.ui;
    if (fileList?.any()) {
      throw new Error("不支持通过filelist传值, 请根据valueEmitType直接设置formdata为blobObjectId或者file对象");
    }
    if (this.ui.valueEmitType == "id") {
      this.injector
        .get(Apollo)
        .query<BlobObjectsQuery, BlobObjectsQueryVariables>({
          query: BlobObjectsGql,
          variables: {
            includeDetail: true,
            where: {
              id: {
                in: value as string[],
              },
            },
          },
        })
        .pipe(
          map(x => {
            // console.log(x);
            let blobs = x.data.blobObjects.items;
            fileList = blobs.map(y => ({
              name: y.fileName,
              uid: y.id,
              size: Number(y.fileSize),
              url: y.url,
              status: "done",
              linkProps: {
                download: y.url,
              },
            }));
            this.fileList = fileList;
            this.setValue(this.schema.transformIn ? this.schema.transformIn(value) : value);
            // this.formProperty._value = this.pureValue(fileList);
            // this.formProperty.updateValueAndValidity({ onlySelf: false, emitValueEvent: false, emitValidator: false });
            // this.detectChanges();
          }),
        )
        .toPromise();
    } else {
      fileList = value as NzUploadFile[];
      this.fileList = fileList;
      this.formProperty._value = this.pureValue(fileList);
      this.formProperty.updateValueAndValidity({ onlySelf: true, emitValueEvent: false, emitValidator: false });
      this.detectChanges(true);
    }
  }
  private _getValue(file: NzUploadFile): NzSafeAny {
    // console.log(this.ui.valueEmitType, file);
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
  handleRemove = () => {
    this._setValue(this.fileList);
    return true;
  };
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
function mergeMap(arg0: (md5: any) => any): import("rxjs").OperatorFunction<string, unknown> {
  throw new Error("Function not implemented.");
}
