# Backups verification

Run from the Geex repository root in PowerShell 7. Backend prerequisites are .NET 10 and `redis-server`, `mongod`, `mongodump`, `mongorestore` on PATH (or `GEEX_BACKUP_MONGOD`, `GEEX_BACKUP_MONGODUMP`, `GEEX_BACKUP_MONGORESTORE`). Frontend prerequisites are the workspace's Node/pnpm versions and installed workspace dependencies. No application database configuration is read by the fixture.

```powershell
pwsh ./scripts/testing/test-backups.ps1
```

Use `-Scope Backend` or `-Scope Frontend` for focused work. Every test stage follows a successful build. Schematics is JavaScript with no build stage. A failed build stops dependent tests. Evidence is centralized in ignored `.test-evidence/database-backup/<run-id>`: build/test logs, `results.trx`, and a local NuGet package. Page compiler output is in `.test-evidence/database-backup/page`. The script never publishes packages.

## Automated coverage

| Area | Expected evidence |
| --- | --- |
| Archive integrity | Real isolated MongoDB export/restore preserves BSON values and indexes and excludes another database. |
| Entity lifecycle | Repeated saves/deserialization do not start another export; callbacks are awaited in order; failed final persistence is compensated. |
| Ownership | Concurrent lease claims have one owner; manual work does not consume scheduled slots; lost ownership/timeout/shutdown cancel work. |
| Automatic switches | All four combinations of module Enabled and background-job Disabled are checked: Cron registration follows both switches; manual execution succeeds in every combination, even without valid automatic-only settings. |
| Retention | A manual run leaves excess automatic backups untouched. A scheduled run retains seven newest successful automatic archives. Older manual backups, records with the source field absent, their BlobObjects/files, and unrelated files survive. Explicit expiration still removes protected archives. |
| Synchronous cleanup | Deletion and final state persistence complete before success. Partial deletion permits explicit retry; failures retain Succeeded with a cleanup error. A real isolated MongoDB failpoint checks failure of the final state update after file/Blob removal. Later backups do not resume failed cleanup. Healthy automatic retention excludes these failures and continues other candidates, while database errors abort retention. Ownership is checked again before final persistence and error recording; manual/automatic cleanup tests replace the lease owner after deletion to verify immediate cancellation. |
| Submission tracking | The returned ID is queryable immediately. Failed historical cleanup does not prevent a new archive. Concurrent work is rejected. |
| Download responses | Real loopback HTTP checks preserve ordinary/archive bytes and headers, return errors before attachment transmission, abort partial/short streams, and dispose cancelled streams. |
| Diagnostics | A real authentication failure persists the tool exit code and sanitized diagnostic; URI/credential redaction and summary length are checked. |
| GraphQL contract | The backups query/mutation fields expose Boolean results, nullable tracking IDs and source, apply Backups permissions, preserve enum values, and validate the frontend package's actual operation documents. |
| Frontend API | A real Apollo client with host `errorPolicy: ignore` still rejects GraphQL errors. Partial responses throw; explicit false/null remains a business rejection. |
| Page | Strict Angular compilation checks the template. Controller tests cover permissions, manual submission with automatic backups disabled, mixed-list submission resets, stale-data failures, cleanup retries, rejection vs failure, and out-of-order refreshes. UI services are mocked; these are not browser end-to-end tests. |
| Schematics | Add/sync generates backup files and preserves locally modified files under the existing conflict rules. |
| Packaging | A local NuGet package is produced; release and prerelease workflow packing lists both include Backups. |

The release workflow runs the backend schema/options/diagnostics tests after building, and builds/tests the frontend page before npm publication. Full backend integration tests require MongoDB tools and run through the script; they are not silently substituted with the smaller release check.

Additional regression coverage includes Redis communication between independent processes, notification type reconstruction, namespace isolation, cache connection reuse, manual-result message idempotence/read preservation, UTC result timestamps and repair of uninitialized timestamps, notification failures, same-route/cached-page actions and subscription lifecycle cleanup.

The consuming PIMS repository also provides `scripts/testing/run-backups-e2e.ps1`. It builds both hosts and the frontend, creates isolated loopback MongoDB/Redis and archive storage, and runs the real page through login, backup, WebSocket reconciliation, result-message navigation, download integrity/restore and synchronous cleanup. Separate scenarios exercise automatic retention, export failures and standalone management APIs. Its dedicated Playwright configuration requires a generated isolated fixture, uses one worker/zero retries, and retains per-run reports, traces, screenshots, failure videos and archive verification evidence. See PIMS `server/BACKUPS.md` for prerequisites and invocation; these browser tests complement the module/controller tests above.

## Host acceptance checks

Use an isolated host with matching backend, frontend package and page template versions. These checks create/delete test archives, so do not point them at production as part of routine automated verification.

1. With automatic execution disabled, history remains queryable and manual start still works. Repeat with the background-job global switch disabled. Verify start/expire controls separately with query-only permissions.
2. Apply a filter that excludes new backups and navigate past page 1. Submit a backup. The page should clear its filters, return to page one and show both manual and automatic records. Manual completion messages received through `onPrivateNotify` silently refresh the list without polling or resetting filters/page. Verify success/failure/cancellation and reconnect readiness; unrelated messages must not refresh it. No refresh timestamp or manual refresh button is shown.
3. Attempt a second submission during execution. It should report a rejected/busy operation. A GraphQL exception should instead report failure.
4. Download a successful archive and verify it in an isolated restore target as described in README.md.
5. Interrupt cleanup after deletion starts. The row retains Succeeded with a cleanup error and no enabled download. A new backup must not resume it. Explicitly retry cleanup; the row becomes Expired with no BlobObject reference/error. There is no intermediate cleanup status.
6. Interrupt connectivity during a same-query silent refresh: keep previous rows, show stale-data feedback, and disable destructive actions. A failed changed filter/page instead shows an error empty state with no old total. Restore connectivity and verify normal refresh resumes.
7. Verify manual/automatic/unknown source labels. Run enough automatic backups to exceed retention: only older automatic archives should disappear. Archives with Manual or Unknown source must remain downloadable; explicitly expire them to confirm manual cleanup still works.
