import { Compute } from "ts-toolbelt/out/Any/Compute";

export type LangObject<O> = O extends object
  ? // A.Compute here isn't necessary, but produces nicer readable types
    Compute<{ get?(x: string, notFoundValue?: string): string } & { [K in keyof Partial<O>]: LangObject<Partial<O>[K]> }>
  : string;
