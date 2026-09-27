import type { GeexModule } from "@geexcode/geex-angular";
import type { DocumentNode } from "graphql";

export type BackupStatus = "Running" | "Succeeded" | "Failed" | "Cancelled" | "Expired";
export type BackupSource = "Unknown" | "Automatic" | "Manual";
export interface Backup {
  id: string;
  databaseName: string;
  scheduledAt: string;
  startedAt: string;
  finishedAt?: string | null;
  status: BackupStatus;
  source: BackupSource;
  errorMessage?: string | null;
  blobObjectId?: string | null;
  file?: { id: string; fileName?: string | null; fileSize: number; url?: string | null } | null;
}
export interface BackupsList {
  backupsEnabled: boolean;
  backups?: { items?: Array<Backup | null> | null; totalCount: number } | null;
}
export interface BackupsVariables {
  skip?: number;
  take?: number;
  filter?: Record<string, unknown>;
}
export interface BackupsModule extends GeexModule<{
  watch(changed: () => void, failed: (error: unknown) => void): { unsubscribe(): void };
  listDocument: DocumentNode;
  startDocument: DocumentNode;
  startTrackedDocument: DocumentNode;
  expireDocument: DocumentNode;
  list(variables: BackupsVariables): Promise<BackupsList>;
  start(): Promise<boolean>;
  startTracked(): Promise<string | null>;
  expire(id: string): Promise<boolean>;
}> {}

declare module "@geexcode/geex-angular" {
  interface GeexModuleMap { backups: BackupsModule; }
}
