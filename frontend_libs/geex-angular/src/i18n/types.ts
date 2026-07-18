export type LangObject<O = Record<string, any>> = O extends object
  ? { get(x: string, notFoundValue?: string): string } & {
      [K in keyof O]: LangObject<O[K]>;
    }
  : string;
