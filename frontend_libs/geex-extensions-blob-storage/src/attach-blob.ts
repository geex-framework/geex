import { defer, from, map, of, switchMap, type Observable } from "rxjs";
import type { BlobStorageModule } from "./blob-storage.types";

export interface BlobAttachment {
  id: string;
  fileName?: string | null;
  fileSize: number;
  md5?: string | null;
  mimeType?: string | null;
  storageType: string;
  url?: string | null;
  expireAt?: string | null;
}

export function attachBlob(
  storage: BlobStorageModule,
  content: Blob,
  fileName: string,
  storageType = storage.defaultStorageType,
): Observable<BlobAttachment> {
  const mimeType = content.type.trim().toLowerCase() || "application/octet-stream";
  const file = new File([content], fileName, { type: mimeType });

  async function findExisting(md5: string): Promise<BlobAttachment | undefined> {
    if (storageType !== "Db" && storageType !== "FileSystem") return undefined;
    let skip = 0;
    const take = 20;
    while (true) {
      const data = await storage.list({
        filter: {
          fileName: { eq: fileName },
          md5: { eq: md5 },
          fileSize: { eq: file.size },
          storageType: { eq: storageType },
        },
        skip,
        take,
      }) as { blobObjects?: {
        items?: Array<BlobAttachment | null>;
        totalCount?: number;
        pageInfo?: { hasNextPage: boolean };
      } } | undefined;
      const page = data?.blobObjects;
      if (!Array.isArray(page?.items)) throw new Error("Blob lookup returned no result.");
      const now = Date.now();
      const existing = page.items.find((blob): blob is BlobAttachment => !!blob?.id &&
        blob.fileName === fileName && blob.md5?.toLowerCase() === md5 && blob.fileSize === file.size &&
        blob.storageType === storageType && blob.mimeType?.trim().toLowerCase() === mimeType &&
        (blob.expireAt == null || Date.parse(blob.expireAt) > now));
      if (existing) return existing;
      skip += page.items.length;
      const hasNextPage = page.pageInfo?.hasNextPage ?? skip < (page.totalCount ?? 0);
      if (!hasNextPage) return undefined;
      if (page.items.length === 0) throw new Error("Blob lookup returned an empty page with more results.");
    }
  }

  return defer(() => from(file.computeChecksumMd5())).pipe(
    switchMap(md5 => {
      const checksum = md5.toLowerCase();
      return from(findExisting(checksum)).pipe(
        switchMap(existing => existing ? of(existing) : from(storage.create({
          request: { file, md5: checksum, storageType },
        }, { useMultipart: true })).pipe(
          map(data => {
            const created = (data as { createBlobObject?: BlobAttachment } | undefined)?.createBlobObject;
            if (!created?.id) throw new Error("Blob upload returned no result.");
            return created;
          }),
        )),
      );
    }),
  );
}
