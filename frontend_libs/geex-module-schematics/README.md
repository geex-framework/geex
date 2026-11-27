# @geexcode/geex-module-schematics

Install Geex admin UI modules as **source** under `src/app/modules/<name>`.

```bash
pnpm geex add identity
pnpm geex sync identity
pnpm geex sync --all
```

- Default path: `src/app/modules`
- Refuses overwrite on `add` unless `--force`
- Overlay modules (including `identity`, `blob-storage`, `backups`, `approval-flows`, `favorites`, `mocking`) install/sync from `files/extensions/<name>` only (no blank scaffold merge)
- `sync` skips existing files unless `--force`
- `sync` without a name (or with `--all`) syncs every module in `.geex/modules.json`
- Updates `module-registry.ts` and `.geex/modules.json` on `add`
- Updates `templateVersion` / `syncedAt` on `sync`

Favorites contributes translations for the shared favorites components in the personal center and approval flow pages. See the [favorites integration guide](../../framework_modules/Geex.Extensions.Favorites/README.md). The `identity` / `approval-flows` / `favorites` installers add the favorites package dependency; run `pnpm install` after generation.
