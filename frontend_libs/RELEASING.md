# Frontend releases

The repository's `.github/workflows/publish.yml` publishes NuGet packages and all
17 public frontend workspace packages from the same Git tag. This includes
`@geexcode/geex-module-schematics`.

- Push `vMAJOR.MINOR.PATCH`, for example `v10.0.10`, for a stable release.
- Push a prerelease tag such as `v10.0.10-rc1` or `v10.0.10-rc.1` for a prerelease.
- Manual runs accept an existing tag, with or without `refs/tags/`. Both jobs
  check out that tag rather than the branch selected for the manual run.
- The tag without `v` is the complete package version for both npm and NuGet.
  Four-component versions and build metadata are rejected to avoid incompatible
  or differently normalized package versions.
- Stable npm releases use `latest`; prereleases use `next`.

Each package uses npm trusted publishing (OIDC) with this configuration:

- Organization: `geex-framework`
- Repository: `geex`
- Workflow filename: `publish.yml`
- Environment: leave empty (the publishing job has no environment)
- Allowed actions: enable `npm publish`

No `NPM_TOKEN` secret is needed. The GitHub-hosted publishing job has
`id-token: write` permission and uses Node.js 22 with npm 11 (at least 11.5.1).
pnpm 10.14.0 packs the workspace packages and invokes the installed npm CLI for
each upload, allowing npm to authenticate through OIDC. NuGet continues to use
its existing OIDC setup. See the [npm trusted publishing documentation](https://docs.npmjs.com/trusted-publishers/).

CI installs the committed pnpm lockfile, sets every public package's version and
internal peer/dependency versions and repository metadata, then builds in dependency order. It checks
the archive contents and versions of every package before starting npm uploads.
Packages are published from their roots, preserving the existing `dist/` entry
paths. Schematics ships its source/templates and CLI entry without a build step.

Version updates happen only in the CI checkout; they are not committed back.
The frontend job uses pnpm's recursive publish to skip versions already on npm,
so rerunning a partially completed release can publish the remaining packages.
NuGet and npm are independent jobs; publication across registries is not atomic.

To validate in a disposable checkout without publishing, run from `frontend_libs`:

```powershell
pnpm install --frozen-lockfile
./release.ps1 -Version 10.0.10-rc1 -Mode Prepare
pnpm -r --workspace-concurrency=1 run build
./release.ps1 -Version 10.0.10-rc1 -Mode Verify
```

These commands modify package manifests and build output in that checkout.
Commit the workflow, release script, workspace sources, lockfile, and the
schematics CLI entry before creating the release tag.
