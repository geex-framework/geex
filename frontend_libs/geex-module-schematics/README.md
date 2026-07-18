# @geexcode/geex-module-schematics

Install Geex admin UI modules as **source** under `src/app/modules/<name>`.

```bash
pnpm geex add identity
pnpm geex sync identity
```

- Default path: `src/app/modules`
- Refuses overwrite on `add` unless `--force`
- `sync` skips existing files unless `--force`
- Updates `module-registry.ts` and `.geex/modules.json` on `add`
