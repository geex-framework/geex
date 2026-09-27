import { Injector } from "@angular/core";
import { Apollo, gql } from "apollo-angular";
import { firstValueFrom } from "rxjs";
import type { BackupsList, BackupsModule } from "./backups.types";

export const BACKUPS_CHANGED = gql`subscription onBackupsChanged { onBackupsChanged }`;

export const BACKUPS = gql`
  query backups($filter: BackupFilterInput, $skip: Int = 0, $take: Int = 10) {
    backupsEnabled
    backups(filter: $filter, skip: $skip, take: $take) {
      items {
        id databaseName scheduledAt startedAt finishedAt status source errorMessage blobObjectId
        file { id fileName fileSize url }
      }
      totalCount
    }
  }
`;
export const START_BACKUP = gql`
  mutation startBackup { startBackup }
`;
export const START_BACKUP_TRACKED = gql`
  mutation startBackupTracked { startBackupTracked }
`;
export const EXPIRE_BACKUP = gql`
  mutation expireBackup($id: String!) { expireBackup(id: $id) }
`;

export function createBackupsModule(injector: Injector): BackupsModule {
  const apollo = () => injector.get(Apollo);
  return {
    watch: (changed, failed) => apollo().use("subscription").subscribe<{ onBackupsChanged: string }>({
      query: BACKUPS_CHANGED, fetchPolicy: "no-cache", errorPolicy: "none",
    }).subscribe({ next: result => { if (result.data?.onBackupsChanged != null) changed(); }, error: failed }),
    listDocument: BACKUPS,
    startDocument: START_BACKUP,
    startTrackedDocument: START_BACKUP_TRACKED,
    expireDocument: EXPIRE_BACKUP,
    list: async variables => {
      const result = await firstValueFrom(apollo().query<BackupsList>({ query: BACKUPS, variables, fetchPolicy: "no-cache", errorPolicy: "none" }));
      if (typeof result.data?.backupsEnabled !== "boolean" ||
          !Array.isArray(result.data.backups?.items) ||
          !Number.isInteger(result.data.backups?.totalCount) || result.data.backups!.totalCount < 0)
        throw new Error("Incomplete database backup data returned.");
      return result.data;
    },
    start: async () => {
      const result = await firstValueFrom(apollo().mutate<{ startBackup: boolean }>({ mutation: START_BACKUP, errorPolicy: "none" }));
      if (typeof result.data?.startBackup !== "boolean") throw new Error("Missing backup start result.");
      return result.data.startBackup;
    },
    startTracked: async () => {
      const result = await firstValueFrom(apollo().mutate<{ startBackupTracked: string | null }>({ mutation: START_BACKUP_TRACKED, errorPolicy: "none" }));
      const id = result.data?.startBackupTracked;
      if (id !== null && (typeof id !== "string" || !id.trim())) throw new Error("Missing backup tracking result.");
      return id;
    },
    expire: async id => {
      const result = await firstValueFrom(apollo().mutate<{ expireBackup: boolean }>({ mutation: EXPIRE_BACKUP, variables: { id }, errorPolicy: "none" }));
      if (typeof result.data?.expireBackup !== "boolean") throw new Error("Missing backup expiration result.");
      return result.data.expireBackup;
    },
    init: async () => undefined,
  };
}
