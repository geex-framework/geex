# @geexcode/geex-module-schematics

Install Geex admin UI modules as **source** under `src/app/modules/<name>`.

```bash
pnpm geex add identity
pnpm geex sync identity
pnpm geex sync --all
```

- Default path: `src/app/modules`
- Refuses overwrite on `add` unless `--force`
- Overlay modules (`identity`, `blob-storage`, `approval-flows`, `mocking`) install/sync from `files/extensions/<name>` only (no blank scaffold merge)
- `sync` skips existing files unless `--force`
- `sync` without a name (or with `--all`) syncs every module in `.geex/modules.json`
- Updates `module-registry.ts` and `.geex/modules.json` on `add`
- Updates `templateVersion` / `syncedAt` on `sync`
