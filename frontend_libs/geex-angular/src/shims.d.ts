declare module "*.js" {
  const value: any;
  export default value;
}

declare module "linqts-camelcase" {
  export class List<T = any> {
    constructor(items?: T[]);
    [key: string]: any;
  }
}

declare module "screenfull" {
  interface Screenfull {
    isEnabled: boolean;
    isFullscreen: boolean;
    request(element?: Element): Promise<void>;
    exit(): Promise<void>;
    toggle(element?: Element): Promise<void>;
    on(event: string, handler: () => void): void;
    off(event: string, handler: () => void): void;
  }
  const screenfull: Screenfull;
  export default screenfull;
}

declare module "kiwi-intl" {
  const kiwiIntl: {
    init(lang: string, langs: Record<string, unknown>): any;
  };
  export default kiwiIntl;
}

declare module "spark-md5" {
  interface SparkMD5ArrayBuffer {
    append(arr: ArrayBuffer): SparkMD5ArrayBuffer;
    end(raw?: boolean): string;
  }
  const SparkMD5: {
    ArrayBuffer: new () => SparkMD5ArrayBuffer;
  };
  export default SparkMD5;
}

declare module "extract-files/extractFiles.mjs" {
  const extractFiles: (value: unknown, isExtractable: (v: unknown) => boolean, path?: string) => unknown;
  export default extractFiles;
}

declare module "extract-files/isExtractableFile.mjs" {
  const isExtractableFile: (value: unknown) => boolean;
  export default isExtractableFile;
}
