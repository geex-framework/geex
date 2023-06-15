import { Component, ContentChild, ElementRef, forwardRef, Injector, Input, OnInit, ViewChild } from "@angular/core";
import { ControlValueAccessor, DefaultValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import { Apollo } from "apollo-angular";
import * as _ from "lodash";
import { NzSafeAny } from "ng-zorro-antd/core/types";
import { NzModalService } from "ng-zorro-antd/modal";
import { NzUploadChangeParam, NzUploadFile, UploadFilter } from "ng-zorro-antd/upload";
import { from, Observable, switchMap } from "rxjs";

import { BlobStorageType, CreateBlobObjectGql } from "../../graphql/.generated/type";

@Component({
  selector: "geex-upload",
  templateUrl: "./geex-upload.component.html",
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => GeexUploadComponent),
      multi: true,
    },
  ],
})
export class GeexUploadComponent implements ControlValueAccessor {
  @ContentChild("uploadButton") uploadButton: any;
  @Input() nzShowUploadList = false;
  @Input() nzDisabled = false;
  @Input() nzFileListL: NzUploadFile[] = [];
  @Input() nzLimit = 0;
  @Input() nzSize = 0;
  @Input() nzFileType = undefined;
  @Input() nzFilter: UploadFilter[] = [];
  @Input() nzListType = "text";
  @Input() nzShowButton = true;
  @Input() nzOpenFileDialogOnClick = true;
  @Input() nzRemove?: (file: NzUploadFile) => boolean | Observable<boolean>;

  disabled = false;
  fileList: NzUploadFile[] = [];

  constructor(private apollo: Apollo, private injector: Injector) {}

  private _valueAccessor: DefaultValueAccessor;
  public set valueAccessor(value: DefaultValueAccessor) {
    this._valueAccessor = value;
  }
  writeValue(obj: any): void {
    debugger;
    this.valueAccessor && this.valueAccessor.writeValue(obj);
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
  onChange = _ => {};
  onTouched = () => {};

  uploadGeexBlobObject = args => {
    return from((args.postFile.slice() as Blob).computeChecksumMd5())
      .pipe(
        switchMap(md5 => {
          args.onProgress({ percent: 50 }, args.file);
          return this.apollo
            .mutate({
              mutation: CreateBlobObjectGql,
              variables: {
                input: {
                  file: args.file as any,
                  md5: md5,
                  storageType: BlobStorageType.Db,
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
  };

  change(args: NzUploadChangeParam): void {
    if (args.type !== "success") return;
    this.fileList = args.fileList;
    this.onChange(args.fileList[0].response.id);
  }

  handleRemove = () => {
    return true;
  };

  handlePreview = async (file: NzUploadFile): Promise<void> => {
    const _url = file.response.thumbUrl || file.response.url;
    if (!_url) {
      return;
    }
    this.injector.get<NzModalService>(NzModalService).create({
      nzContent: `<img src="${_url}" class="img-fluid" />`,
      nzFooter: null,
    });
  };
}
