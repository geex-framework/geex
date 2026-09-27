# Geex Backups

Headless Angular module for `Geex.Extensions.Backups`.

## Integration

Register `provideGeexBackups()` with the host application's Geex providers. The provider adds `geex.backups` and supports a custom factory through `GeexBackupsOptions.createBackupsModule`. The default factory is `createBackupsModule`; the configuration token is `GEEX_BACKUPS_OPTIONS`.

The exported record type is `Backup`. The module, list and query-variable types are `BackupsModule`, `BackupsList` and `BackupsVariables`.

Install the page source with `geex add backups`. Include its menu, routes and zh-CN/en-US translations in the host registry. The route is `/backups` and the translation namespace is `Backups`. Use `geex sync backups` to synchronize the installed source, reviewing local conflicts. Package installation and page-source synchronization are separate operations.

For local source dependencies, build the package before installing or building the consuming application.

For manual npm publication, first run `./release.ps1 -Version <version> -Mode Prepare`, `pnpm -r --workspace-concurrency=1 run build`, and `./release.ps1 -Version <version> -Mode Verify` from `frontend_libs`. Then run `pnpm --dir geex-extensions-backups publish:public` from that directory. Publish from the package root: its manifest exports and `files` list include the built `dist` directory; publishing `./dist` with that manifest omits the exported code.

## API and permissions

| Frontend method | GraphQL operation | Result | Permission |
| --- | --- | --- | --- |
| `geex.backups.list(variables)` | `backups` / `backupsEnabled` | Paginated records and automatic scheduling status | `Backups_query_backups` |
| `geex.backups.start()` | `startBackup` | Boolean acceptance result | `Backups_mutation_startBackup` |
| `geex.backups.startTracked()` | `startBackupTracked` | Persisted record ID, or null when busy/stopping | `Backups_mutation_startBackup` |
| `geex.backups.expire(id)` | `expireBackup` | Boolean expiration result | `Backups_mutation_expireBackup` |

Grant these permissions through host role management and generate the host's `AppPermission` enum from the backend schema. Queries bypass the Apollo cache. All operations use `errorPolicy: "none"` so GraphQL errors propagate. Missing required response fields are errors.

A successful start response acknowledges background execution. Subscribe to changes and reload history for the final result. Automatic scheduling switches do not disable manual backups. Expiration waits for deletion and the final state save, retains history and clears the BlobStorage association.

## Management page

The page provides paginated history, database/status/trigger-time filters, silent subscription refresh, archive download, manual execution and confirmed cleanup. Accepted manual submissions and the message view action reset the filters and return to the first page of the mixed history. There is no manual refresh control or refresh timestamp.

Statuses are `Running`, `Succeeded`, `Failed`, `Cancelled` and `Expired`. Cleanup goes directly from `Succeeded` to `Expired` after deletion. A cleanup error leaves `Succeeded` and records the error summary; downloading is disabled and explicit cleanup can be retried. Starting another backup never resumes that cleanup.

Draft inputs are separate from submitted filters. Date ranges include the selected final minute and use browser-local time. New-filter or page failures show an error empty state rather than mismatched old rows; a failed silent refresh of the same query retains its rows with a warning. Destructive cleanup is blocked while history is stale, but permitted manual starts remain available even before history loads or after a query failure. Shrinking results correct an out-of-range page before displaying data.

Confirmation displays the selected archive. Only the relevant action shows loading; pending mutations prevent duplicate submission or misleading cancellation, and list refresh does not extend their loading state. Leaving the page closes its dialog and invalidates late results without claiming a server operation was cancelled.

Record sources are `Automatic`, `Manual` and `Unknown`. Missing sources are represented as `Unknown`. Rolling retention counts and expires only successful automatic backups without cleanup errors. Manual submission does not trigger rolling cleanup; archives with `Manual` or `Unknown` source require explicit expiration. The page displays the source and retention rule.

Archive downloads use BlobStorage URLs. Restore is documented in the [backend README](../../framework_modules/Geex.Extensions.Backups/README.md#restore) and is not exposed as a page action.

## Verification

From the Geex repository root:

```powershell
pwsh ./scripts/testing/test-backups.ps1 -Scope Frontend
```

The script builds the package and workspace dependencies, compiles the page with strict Angular template checking, and runs API/page behavior and schematics tests. Logs are stored under ignored `.test-evidence/database-backup/<run-id>` and template compiler output under `.test-evidence/database-backup/page`. The PIMS host additionally runs real browser workflows through its dedicated `scripts/testing/run-backups-e2e.ps1` isolated environment. See the [test guide](../../framework_modules/Geex.Extensions.Backups/TESTING.md) for host acceptance checks.
