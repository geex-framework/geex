# Backups

`BackupsModule` depends on `BackgroundJobModule`, `BlobStorageModule` and `MessagingModule`. Add it to the application's module dependencies and reference the project/package. Manual backups are always available when the module is loaded. `BackupsModuleOptions.Enabled` defaults to false and controls only automatic Cron job registration. `BackgroundJobModuleOptions.Disabled` also suppresses automatic registration without disabling manual backups.

## Configuration

```json
{
  "GeexCoreModuleOptions": {
    "ConnectionString": "mongodb://localhost:27017/<database-name>",
    "AppName": "my-application",
    "Redis": { "Hosts": [{ "Host": "localhost", "Port": 6379 }], "Database": 0 }
  },
  "BackupsModuleOptions": {
    "Enabled": true,
    "WorkingDirectory": "App_Data/backup",
    "MongodumpPath": "mongodump",
    "RetentionCount": 7,
    "Timeout": "02:00:00"
  },
  "BackgroundJobModuleOptions": {
    "Disabled": false,
    "JobConfigs": {
      "BackupsJob": "0 0 3 * * *"
    }
  }
}
```

The connection string must explicitly select the application's business database. The module refuses database-less URLs and the `admin`/`config`/`local` databases. It does not infer or export other databases. MongoDB Database Tools must be installed on the application host; configure an absolute `MongodumpPath` if necessary. The tool must support `--config`, `--archive` and `--gzip`.

## Entity lifecycle

`BackupsExecution` is always registered as a singleton with a hosted shutdown lifecycle; it has no timer and starts no backups at application startup. `BackupsJob` is only registered when automatic scheduling is enabled and delegates scheduled occurrences to that same execution instance. The execution instance acquires the database lease before constructing `Backup`. Construction and expiration are internal: applications start work through `BackupsExecution` or GraphQL so they cannot bypass lease ownership and compensation. The constructor attaches the entity and starts one private asynchronous task. `PreSaveChanges` awaits that task and applies the successful result. Repeated saves do not start another export. Deserialization never starts a task.

The task uses a separate scope for the running record and BlobObject, without recursively saving its caller's unit of work. A non-public constructor supports materialization. Query records with `uow.Query<Backup>()`; no backup-specific request/handler/service is added.

## Management

The `backups` GraphQL query provides offset pagination, database/status/scheduled-time filters and archive metadata. Results are ordered by newest scheduled time. The `backupsEnabled` field reports automatic scheduling status only. It must not be used to gate manual actions. History and manual mutations remain registered regardless of the automatic switches. Execution still requires a valid business-database URI, export tool, working directory and timeout. Cron/retention settings are only required for automatic scheduling.

`startBackup` returns a Boolean indicating whether execution was accepted. `startBackupTracked` returns the persisted Backup ID, or null when busy/stopping. Both acknowledge only after the Running record has been inserted. Recovery of earlier interrupted backup execution runs after that insertion. Acceptance does not mean completion: subscribe to `onBackupsChanged` and reload the mixed history list. The subscription requires the backup query permission and emits an initial event after registration, including reconnection. Manual and scheduled execution share the same database lease, timeout, cancellation and compensation logic. Only a successful automatic execution triggers rolling retention. Manual execution does not consume a scheduled slot. `expireBackup(id)` acquires the lease before reading the current record, waits for file/Blob deletion and the final `Expired` save, then returns true. Already expired records are idempotent; false means another execution currently holds the lease. There is no background cleanup queue or intermediate cleanup state. Restore is not exposed through the API.

Permissions follow the existing Geex module conventions: `Backups_query_backups`, `Backups_mutation_startBackup` and `Backups_mutation_expireBackup`. Both start mutations use `Backups_mutation_startBackup`. The frontend `@geexcode/geex-extensions-backups` package and `backups` source template provide the corresponding management page, with archive downloads using BlobStorage URLs.

The record includes database, scheduled/start/end timestamps, status, source, error summary and BlobObjectId. `Backup.Source` is persisted with the initial Running record: `Automatic` for scheduled execution and `Manual` for manual execution. Records without a source deserialize as `Unknown`; their source is not inferred. The page displays these sources. `Backup.File` resolves the associated `IBlobObject`. File name, length and checksum belong to BlobStorage.

The export writes a gzip-compressed MongoDB archive under `App_Data/backup/<backup-id>`. The URI is passed through a temporary tool configuration file, not command arguments. Standard output is drained; only the last 8192 characters of standard error are retained in memory. Error summaries redact MongoDB URIs and configured credentials, normalize whitespace and limit diagnostic text to 2048 characters plus a truncation marker. Records identify the failed stage: creation, interrupted-work recovery, export or storage. A zero exit code and non-empty archive are required. The archive is streamed to `BlobStorageType.FileSystem`, normally `App_Data/BlobStorageFiles/<md5>`, and the temporary directory is removed. Archive bytes are never stored in MongoDB.

Success requires the file write, BlobObject save and final Backup save to complete. The job compensates failed final saves using a fresh scope. Failed compensation is logged and retried during the next owned execution. A crash can leave a Running record; the next owned execution cleans its file and temporary directory and marks it failed. BlobStorage records the final file address before publishing the file and names incomplete writes `<blob-id>.upload` so they can be recovered through the BlobObject lifecycle.

## Scheduling and retention

- The configured Cron expression uses the hosting process or container local timezone (`TimeZoneInfo.Local`), just like other Cron jobs. The configuration above runs daily at 03:00 local time; a UTC deployment therefore runs at 03:00 UTC. Instances scheduling backups for the same database must use the same local timezone. Execution timestamps and scheduled-slot deduplication remain UTC-based.
- The application must be running. Missed schedules are not replayed. Failed slots are not retried immediately.
- The whole job has a two-hour default timeout. Cancellation kills the export process and waits for it before removing temporary files.
- A MongoDB lease is keyed by database, renewed every 20 seconds and expires after two minutes. Each scheduled occurrence is claimed once. Lost ownership cancels the execution. Application instances must keep their clocks synchronized.
- Multiple instances sharing one database must also share the same persistent BlobStorage directory. A process-local file directory is not shared merely because the metadata is in MongoDB.
- Only a successful automatic backup triggers retention. `RetentionCount` (default seven) counts only successful `Automatic` backups without cleanup errors for the same database, retaining the newest by scheduled time. Backups with `Manual` or `Unknown` source are excluded from both counting and deletion. Manual execution never triggers rolling cleanup. A file-deletion failure is recorded on that archive and does not block other candidates or the next backup. Failed cleanup records neither consume the healthy retention quota nor participate in automatic retries.
- Expiration deletes the archive and BlobObject before marking the backup `Expired` and clearing its file association and error. Execution history remains available. A cleanup failure preserves `Succeeded` (the archive creation outcome) and its association, records a sanitized error, and disables page downloads. Only an explicit cleanup retry completes it; later backup recovery does not resume cleanup. Missing files/BlobObjects count as completed deletion steps. A crash between file deletion and the final save can leave `Succeeded`; an explicit retry finishes that record. Backup execution failure/interruption compensation is unchanged.
- Records marked `Succeeded` with missing files are not automatically reclassified. Explicitly expire those records after checking the storage location.

The status contract is `Running`, `Succeeded`, `Failed`, `Cancelled`, `Expired`, with their original numeric values preserved. Older `Expiring` records are not migrated or mapped by this version and must be absent or handled separately before deployment. The download endpoint opens a stream before committing attachment headers; errors before transmission return a non-success response, and interrupted transfers abort rather than completing a truncated archive.

This is an online, single-database logical backup. It does not use `--oplog` and does not guarantee that related collections represent one instant while writes continue. It does not copy external file attachments or provide continuous point-in-time recovery. Files stored on the same machine are also lost if that machine's storage is lost.

## Restore

Stop automatic backups on the recovery application until the archive has been inspected. Locate a successful Backup record's BlobObject and its MD5 file. If the application database is unavailable, existing physical files in BlobStorage are still gzip MongoDB archives; their hash file names do not prevent restoration. Use an isolated MongoDB instance first.

PowerShell example, with the original and destination database names explicitly selected:

```powershell
mongorestore --uri 'mongodb://127.0.0.1:27018' --gzip `
  --archive 'D:/recovery/BlobStorageFiles/<md5>' `
  --nsInclude '<database-name>.*' --nsFrom '<database-name>.*' --nsTo '<database-name>_restore.*'
```

Verify document counts, important BSON values and indexes before switching the application to the restored database. The archive contains historical Backup/BlobObject metadata, not copies of filesystem blobs. This metadata alone does not establish that an older physical file still exists. No automatic restore or production overwrite command is provided.

## Tests

The test project starts its own mongod on a randomly selected loopback port and uses fresh databases and directories. It never reads the application's configured database. `redis-server`, `mongod`, `mongodump` and `mongorestore` must be on PATH; alternatively set `GEEX_BACKUP_MONGOD`, `GEEX_BACKUP_MONGODUMP` and `GEEX_BACKUP_MONGORESTORE` to executable paths.

For standalone verification from the Geex checkout, the maintained script builds before testing and keeps logs/results/local packages under ignored `.test-evidence/database-backup/<run-id>`:

```powershell
pwsh ./scripts/testing/test-backups.ps1
# Backend-only or frontend-only verification:
pwsh ./scripts/testing/test-backups.ps1 -Scope Backend
pwsh ./scripts/testing/test-backups.ps1 -Scope Frontend
```

See [TESTING.md](TESTING.md) for prerequisites, coverage and manual checks. Both release and prerelease NuGet packing lists include this module. The verification script creates a local package only; it never publishes or starts a backup against an application database.

## Distributed notifications

Redis configuration is mandatory. HotChocolate 13.9.14 Redis subscriptions borrow a connection from the existing framework cache pool. All topics use an application/environment/business database/Redis database namespace. Coordinate a restart of all backend instances when switching providers; do not mix in-memory and Redis backends. Pub/Sub messages are transient, so pages reload persisted state on initial subscription and reconnect. Client notification transport contains a serializable envelope (type/time/message ID or change category), never an Entity or unit of work. The receiving instance resolves Message entities from its own database scope.

Manual requests capture the requester before starting background execution and preallocate a result message ID. Terminal results create an insert-only Message and targeted MessageDistribution in an independent scope, then mark message persistence complete and publish the existing private notification. Result messages and distributions explicitly store the backup completion time in UTC. Recovery repairs minimum timestamps left by older direct inserts without changing valid timestamps, read state or resending messages. Retries preserve existing read state. Startup and the normal recovery path retry incomplete persistence. Historical backups lack these optional fields and never generate retrospective messages. Automatic backups and programmatic requests without a requester never produce user messages. Notification failures cannot invalidate a backup or delete its archive.

The frontend listens to manual completion messages through the existing messaging `onPrivateNotify` subscription and uses lightweight `onBackupsChanged` events for all sources, coalesces bursts and follows an in-flight query with another query when needed. Filters and pagination are preserved for these events. Manual acceptance and the registered `backups.view` message action reset the complete list to page one, including cached/same-route pages. Updates are silent, with no refresh timestamp or manual refresh button. Private subscription readiness also reconciles state after reconnect, and page listeners are released on leave. There is no polling or percentage progress. Message actions are registered by business modules through the generic messaging registry; actions must succeed before the message is marked read.
