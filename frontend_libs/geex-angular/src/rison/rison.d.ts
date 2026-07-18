export = rison;
export as namespace rison;

declare const rison: IRison;

interface IRison {
  encode(obj: any): string;
  encode_object<T>(obj: T): string;
  encode_array<T>(arr: T[]): string;
  encode_uri(obj: any): string;
  decode<T = any>(encoded: string): T;
  decode_query_param(encoded: string | null | undefined): string;
  decode_object<T>(encoded: string): T;
  decode_array<T>(encoded: string): T[];
}
