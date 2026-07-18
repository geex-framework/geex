/// <reference path="../shims.d.ts" />
import SparkMD5 from "spark-md5";

declare global {
  interface Blob {
    computeChecksumMd5(this: Blob): Promise<string>;
  }
}

function computeChecksumMd5(this: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const chunkSize = 2097152;
    const spark = new SparkMD5.ArrayBuffer();
    const fileReader = new FileReader();
    let cursor = 0;

    fileReader.onerror = (): void => {
      reject("MD5 computation failed - error reading the file");
    };

    const processChunk = (chunkStart: number) => {
      const chunkEnd = Math.min(this.size, chunkStart + chunkSize);
      fileReader.readAsArrayBuffer(this.slice(chunkStart, chunkEnd));
    };

    fileReader.onload = (e: ProgressEvent<FileReader>): void => {
      spark.append(e.target!.result as ArrayBuffer);
      cursor += chunkSize;

      if (cursor < this.size) {
        processChunk(cursor);
      } else {
        resolve(spark.end());
      }
    };

    processChunk(0);
  });
}

Blob.prototype.computeChecksumMd5 = computeChecksumMd5;
