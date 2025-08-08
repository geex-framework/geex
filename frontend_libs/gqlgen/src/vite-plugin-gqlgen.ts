import type { Plugin } from 'vite';
import path from 'node:path';
import fs from 'node:fs';
import { generate } from '@graphql-codegen/cli';

export type GqlgenOptions = {
  /**
   * Where to emit shared types like schema types and fragments.
   * Defaults to `${sourceRoot}/graphql`.
   */
  sharedTypesDir?: string;
  /**
   * Additional custom scalar mappings to merge with defaults.
   * Example: { URL: 'string' }
   */
  scalars?: Record<string, string>;
  /**
   * Optional local schema file(s). Relative paths are resolved from project root.
   */
  schemaLocal?: string | string[];
  /**
   * Optional remote schema URL(s).
   */
  schemaRemote?: string | string[];
};

type ViteState = {
  projectRoot: string;
  sourceRoot: string;
};

const DEFAULT_SCALARS: Record<string, string> = {
  ChinesePhoneNumber: 'string',
  DateTime: 'Date',
  Decimal: 'number',
  Long: 'BigInt',
  ObjectId: 'string',
  MimeType: 'string',
};

function toPosix(p: string): string {
  return p.replace(/\\/g, '/');
}

function resolveSourceRoot(projectRoot: string): string {
  const tsconfigPath = path.join(projectRoot, 'tsconfig.json');
  try {
    if (fs.existsSync(tsconfigPath)) {
      const json = JSON.parse(fs.readFileSync(tsconfigPath, 'utf8'));
      const compilerOptions = json?.compilerOptions ?? {};
      const rootDir: string | undefined = compilerOptions.rootDir || compilerOptions.sourceRoot;
      if (rootDir && typeof rootDir === 'string') {
        return toPosix(path.isAbsolute(rootDir) ? rootDir : path.join(projectRoot, rootDir));
      }
    }
  } catch {
    // Fallback below
  }
  return toPosix(path.join(projectRoot, 'src'));
}

function buildDocumentsGlobsRelative(sourceRootRel: string): string[] {
  const s = sourceRootRel.replace(/\\/g, '/').replace(/\/$/, '');
  const base = s === '' || s === '.' ? '' : `${s}/`;
  return [
    `${base}**/*.{ts,tsx,vue,gql}`,
    `!${base}**/*.{test,spec}.*`,
    `!${base}**/*.gql.ts`,
  ];
}

function ensureDotPrefix(p: string): string {
  return p.startsWith('.') ? p : `./${p}`;
}

function computeBaseTypesImportPath(sourceRootAbs: string, sharedTypesDirAbs: string): string {
  const relativeFromSourceRoot = toPosix(
    path.relative(sourceRootAbs, path.join(sharedTypesDirAbs, 'schema.gql.ts')),
  );
  return ensureDotPrefix(relativeFromSourceRoot);
}

function isHttpUrl(value: string): boolean {
  return /^(https?:)?\/\//i.test(value);
}

function normalizeToArray<T>(value?: T | T[]): T[] {
  if (value === undefined) return [];
  return Array.isArray(value) ? value : [value];
}

function readRcSchemas(projectRoot: string): string[] {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  if (!fs.existsSync(rcPath)) return [];
  try {
    const content = fs.readFileSync(rcPath, 'utf8');
    const lines = content.split(/\r?\n/);
    const results: string[] = [];
    let inSchema = false;
    for (const rawLine of lines) {
      const line = rawLine.replace(/\t/g, '  ');
      if (!inSchema) {
        if (/^schema:\s*$/.test(line)) {
          inSchema = true;
        }
        continue;
      }
      // Stop if we hit another top-level key
      if (/^[A-Za-z0-9_\-]+:\s*/.test(line)) break;
      const match = line.match(/^\s*-\s*(.+)\s*$/);
      if (match) {
        const entry = match[1].trim();
        if (entry) results.push(entry);
      }
    }
    return results;
  } catch {
    return [];
  }
}

function resolveSchemas(projectRoot: string, sourceRootAbs: string, opts: GqlgenOptions): string[] {
  // 1) Explicit options have top priority
  const locals = normalizeToArray(opts.schemaLocal).map((p) =>
    toPosix(path.isAbsolute(p) || isHttpUrl(p) ? p : path.join(projectRoot, p)),
  );
  const remotes = normalizeToArray(opts.schemaRemote);
  const fromOptions = [...locals, ...remotes];
  if (fromOptions.length > 0) return fromOptions;

  // 2) Respect .graphqlrc.yml if present
  const fromRc = readRcSchemas(projectRoot).map((p) =>
    isHttpUrl(p) ? p : toPosix(path.isAbsolute(p) ? p : path.join(projectRoot, p)),
  );
  if (fromRc.length > 0) return fromRc;

  // 3) Fallback defaults
  const schemaLocalDefault = toPosix(path.join(sourceRootAbs, 'gql', 'schema.graphql'));
  const defaults: string[] = [];
  if (fs.existsSync(schemaLocalDefault)) defaults.push(schemaLocalDefault);
  defaults.push('https://api.dev.geexcode.com/graphql');
  return defaults;
}

function walkDirectoryCollectFiles(rootDir: string): string[] {
  const results: string[] = [];
  const stack: string[] = [rootDir];
  while (stack.length) {
    const current = stack.pop()!;
    const entries = fs.readdirSync(current, { withFileTypes: true });
    for (const entry of entries) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(full);
      } else if (entry.isFile()) {
        results.push(toPosix(full));
      }
    }
  }
  return results;
}

function assertNoGraphqlDocuments(sourceRootAbs: string, allowedGraphqlPathsAbs: Set<string>): void {
  const allFiles = walkDirectoryCollectFiles(sourceRootAbs);
  const offending = allFiles.filter((p) => p.endsWith('.graphql') && !allowedGraphqlPathsAbs.has(p));
  if (offending.length > 0) {
    const list = offending.map((p) => ` - ${p}`).join('\n');
    throw `.graphql documents are not allowed. Use .gql extension for documents.\nAllowed .graphql files are schema files only.\nOffending files:\n${list}`;
  }
}

const gqlgen = (options: GqlgenOptions = {}): Plugin => {
  const state: ViteState = { projectRoot: '', sourceRoot: '' };

  return {
    name: 'vite-plugin-gqlgen',
    enforce: 'pre',

    configResolved(config) {
      const projectRoot = toPosix(config.root || process.cwd());
      const sourceRoot = resolveSourceRoot(projectRoot);
      state.projectRoot = projectRoot;
      state.sourceRoot = sourceRoot;
    },

    async buildStart() {
      try {
        const projectRoot = state.projectRoot || toPosix(process.cwd());
        const sourceRootAbs = state.sourceRoot || resolveSourceRoot(projectRoot);
        const sharedTypesDirAbs = toPosix(
          options.sharedTypesDir
            ? path.isAbsolute(options.sharedTypesDir)
              ? options.sharedTypesDir
              : path.join(projectRoot, options.sharedTypesDir)
            : path.join(sourceRootAbs, 'graphql'),
        );

        const sourceRootRelRaw = path.relative(projectRoot, sourceRootAbs);
        const sourceRootRel = sourceRootRelRaw === '' ? '.' : toPosix(sourceRootRelRaw);
        const documents = buildDocumentsGlobsRelative(sourceRootRel);
        const baseTypesImportPath = computeBaseTypesImportPath(sourceRootAbs, sharedTypesDirAbs);
        const scalars = { ...DEFAULT_SCALARS, ...(options.scalars || {}) };

        const schemaEntries = resolveSchemas(projectRoot, sourceRootAbs, options);
        // Build whitelist of allowed .graphql paths (schema entries that are local files)
        const allowedGraphqlPathsAbs = new Set(
          schemaEntries
            .filter((s) => !isHttpUrl(s))
            .map((s) => (path.isAbsolute(s) ? toPosix(s) : toPosix(path.join(projectRoot, s)))),
        );
        // Enforce no .graphql documents (only .gql allowed) except whitelisted schema files
        assertNoGraphqlDocuments(sourceRootAbs, allowedGraphqlPathsAbs);

        const codegenConfig = {
          overwrite: true,
          schema: schemaEntries,
          documents,
          config: {
            onlyOperationTypes: true,
            enumsAsTypes: false,
            declarationKind: 'interface',
            omitOperationSuffix: true,
            documentMode: 'graphQLTag',
            defaultScalarType: 'any',
            operationResultSuffix: 'Result',
            documentVariableSuffix: '',
            fragmentVariableSuffix: '',
            namingConvention: 'keep',
            scalars,
          },
          generates: {
            [toPosix(path.join(sharedTypesDirAbs, 'schema.gql.ts'))]: {
              plugins: ['typescript'],
            },
            [toPosix(path.join(sourceRootAbs))]: {
              preset: 'near-operation-file',
              presetConfig: {
                extension: '.gql.ts',
                baseTypesPath: baseTypesImportPath,
              },
              plugins: ['typescript-operations', 'typed-document-node'],
            },
            [toPosix(path.join(sharedTypesDirAbs, 'apollo-helpers.g.ts'))]: {
              plugins: ['typescript-apollo-client-helpers'],
            },
          },
        } as const;

        await generate(codegenConfig as any, true);
      } catch (error) {
        console.error('Failed to run gqlgen:', error);
      }
    },

    resolveId(id, importer) {
      // Support importing raw .gql in user code by redirecting to generated .gql.ts
      if (id.endsWith('.gql')) {
        const stripQuery = (p: string) => p.split('?')[0].split('#')[0];
        let baseDir: string = state.projectRoot || process.cwd();
        if (importer) {
          const importerPath = stripQuery(importer);
          baseDir = path.dirname(importerPath);
        }
        const rawResolved = id.startsWith('.') || id.startsWith('/')
          ? path.resolve(id.startsWith('/') ? (state.projectRoot || process.cwd()) : baseDir, id)
          : path.resolve(baseDir, id);
        const candidateTs = `${rawResolved}.ts`;
        if (fs.existsSync(candidateTs)) {
          return toPosix(candidateTs);
        }
        // If the generated file is missing, surface a helpful error
        throw  `Importing "${id}" requires its generated neighbor "${id}.ts". \n It was not found at ${toPosix(candidateTs)}. Run the build/dev server to generate code, or check your gql file path.`;
      }
      return null;
    },
  };
};

export default gqlgen;
