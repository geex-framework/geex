import type { Plugin } from 'vite';
import path from 'node:path';
import fs from 'node:fs';
import { generate } from '@graphql-codegen/cli';
import { buildClientSchema, getIntrospectionQuery, printSchema } from 'graphql';

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
   * Map of remote schema URL to local schema file path (relative to project root).
   * The plugin will download schemas for these URLs to the target file paths
   * and ensure those files are listed in .graphqlrc.yml's schema list.
   */
  localSchemaMap?: Record<string, string>;
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

// Required documents globs to ensure exist in .graphqlrc.yml
const REQUIRED_DOCUMENTS_GLOBS_UNQUOTED: readonly string[] = [
  "src/**/*.{graphql,gql,js,ts,jsx,tsx,vue}",
  "!src/**/*.test.{graphql,gql,js,ts,jsx,tsx,vue}",
  "!src/**/*.gql.{js,ts}",
] as const;

const REQUIRED_DOCUMENTS_GLOBS_WRITABLE: readonly string[] = [
  "'src/**/*.{graphql,gql,js,ts,jsx,tsx,vue}'",
  "'!src/**/*.test.{graphql,gql,js,ts,jsx,tsx,vue}'",
  "'!src/**/*.gql.{js,ts}'",
] as const;

const REQUIRED_DOCUMENTS_MAP: Readonly<Record<string, string>> = {
  [REQUIRED_DOCUMENTS_GLOBS_UNQUOTED[0]]: REQUIRED_DOCUMENTS_GLOBS_WRITABLE[0],
  [REQUIRED_DOCUMENTS_GLOBS_UNQUOTED[1]]: REQUIRED_DOCUMENTS_GLOBS_WRITABLE[1],
  [REQUIRED_DOCUMENTS_GLOBS_UNQUOTED[2]]: REQUIRED_DOCUMENTS_GLOBS_WRITABLE[2],
};

function stripSurroundingQuotes(value: string): string {
  if (!value) return value;
  const first = value[0];
  const last = value[value.length - 1];
  if ((first === "'" && last === "'") || (first === '"' && last === '"')) {
    return value.substring(1, value.length - 1);
  }
  return value;
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

type GraphqlRcParsed = {
  content: string;
  schemaEntries: string[];
  schemaBlockRange?: { start: number; end: number };
  documentsEntries: string[];
  documentsBlockRange?: { start: number; end: number };
};

function parseGraphqlRc(projectRoot: string): GraphqlRcParsed | undefined {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  if (!fs.existsSync(rcPath)) return undefined;
  const content = fs.readFileSync(rcPath, 'utf8');
  const lines = content.split(/\r?\n/);

  const schemaEntries: string[] = [];
  const documentsEntries: string[] = [];
  let inSchema = false;
  let schemaStart = -1;
  let schemaEnd = -1;
  let inDocuments = false;
  let documentsStart = -1;
  let documentsEnd = -1;

  for (let i = 0; i < lines.length; i++) {
    const rawLine = lines[i];
    const line = rawLine.replace(/\t/g, '  ');
    const topKeyMatch = line.match(/^([A-Za-z0-9_\-]+):\s*(.*)$/);
    if (topKeyMatch) {
      const key = topKeyMatch[1];
      // close any open block before starting a new top-level block
      if (inSchema && schemaEnd === -1) {
        schemaEnd = i - 1;
        inSchema = false;
      }
      if (inDocuments && documentsEnd === -1) {
        documentsEnd = i - 1;
        inDocuments = false;
      }
      if (key === 'schema') {
        inSchema = true;
        schemaStart = i;
        continue;
      }
      if (key === 'documents') {
        inDocuments = true;
        documentsStart = i;
        continue;
      }
      continue;
    }

    if (inSchema) {
      const m = line.match(/^\s*-\s*(.+)\s*$/);
      if (m) {
        const entry = m[1].trim();
        if (entry) schemaEntries.push(entry);
      }
    }
    if (inDocuments) {
      const m = line.match(/^\s*-\s*(.+)\s*$/);
      if (m) {
        const entry = m[1].trim();
        if (entry) documentsEntries.push(entry);
      }
    }
  }
  if (inSchema && schemaEnd === -1) schemaEnd = lines.length - 1;
  if (inDocuments && documentsEnd === -1) documentsEnd = lines.length - 1;

  return {
    content,
    schemaEntries,
    schemaBlockRange: schemaStart >= 0 ? { start: schemaStart, end: schemaEnd } : undefined,
    documentsEntries,
    documentsBlockRange: documentsStart >= 0 ? { start: documentsStart, end: documentsEnd } : undefined,
  };
}

function writeGraphqlRcSchemaBlock(projectRoot: string, parsed: GraphqlRcParsed, newSchemaEntries: string[]): void {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  const lines = parsed.content.split(/\r?\n/);

  const blockLines = ['schema:', ...newSchemaEntries.map((e) => `  - ${e}`)];

  if (parsed.schemaBlockRange) {
    const { start, end } = parsed.schemaBlockRange;
    const before = lines.slice(0, start);
    const after = lines.slice(end + 1);
    const next = [...before, ...blockLines, ...after];
    fs.writeFileSync(rcPath, next.join('\n'), 'utf8');
  } else {
    // Append at end with a separating newline
    const needSep = parsed.content.length > 0 && !/\n$/.test(parsed.content);
    const next = parsed.content + (needSep ? '\n' : '') + blockLines.join('\n') + '\n';
    fs.writeFileSync(rcPath, next, 'utf8');
  }
}

function writeGraphqlRcDocumentsBlock(projectRoot: string, parsed: GraphqlRcParsed, newDocumentsEntries: string[]): void {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  const lines = parsed.content.split(/\r?\n/);

  const blockLines = ['documents:', ...newDocumentsEntries.map((e) => `  - ${e}`)];

  if (parsed.documentsBlockRange) {
    const { start, end } = parsed.documentsBlockRange;
    const before = lines.slice(0, start);
    const after = lines.slice(end + 1);
    const next = [...before, ...blockLines, ...after];
    fs.writeFileSync(rcPath, next.join('\n'), 'utf8');
  } else {
    // Append at end with a separating newline
    const needSep = parsed.content.length > 0 && !/\n$/.test(parsed.content);
    const next = parsed.content + (needSep ? '\n' : '') + blockLines.join('\n') + '\n';
    fs.writeFileSync(rcPath, next, 'utf8');
  }
}

function writeNewGraphqlRcWithSchemaEntries(projectRoot: string, entries: string[]): void {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  const lines: string[] = [];
  lines.push('schema:');
  for (const e of entries) lines.push(`  - ${e}`);
  // also write default documents block
  lines.push('documents:');
  for (const e of REQUIRED_DOCUMENTS_GLOBS_WRITABLE) lines.push(`  - ${e}`);
  const content = lines.join('\n') + '\n';
  fs.writeFileSync(rcPath, content, 'utf8');
}

function domainFromUrl(urlString: string): string | undefined {
  try {
    const u = new URL(urlString);
    return u.hostname;
  } catch {
    return undefined;
  }
}

function domainFromFilePath(filePath: string): string | undefined {
  try {
    const base = path.basename(filePath);
    const withoutExt = base.replace(/\.graphql$/i, '').replace(/\.gql$/i, '');
    const withoutSchema = withoutExt.replace(/\.schema$/i, '');
    // Basic heuristic: return as-is
    return withoutSchema || undefined;
  } catch {
    return undefined;
  }
}

async function fetchRemoteSchemaSdl(urlString: string): Promise<string> {
  const introspectionQuery = getIntrospectionQuery();
  const response = await fetch(urlString, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      'accept': 'application/json',
    },
    body: JSON.stringify({ query: introspectionQuery }),
  });
  if (!response.ok) {
    const text = await response.text().catch(() => '');
    throw new Error(`Failed to fetch GraphQL schema from ${urlString}: ${response.status} ${response.statusText}. ${text}`);
  }
  const json = await response.json();
  if (json.errors) {
    throw new Error(`Introspection error from ${urlString}: ${JSON.stringify(json.errors)}`);
  }
  const sdl = printSchema(buildClientSchema(json.data));
  return sdl;
}

// Removed interactive prompts per requirements; schema sync is driven solely by vite config options

function resolveSchemas(projectRoot: string, sourceRootAbs: string): string[] {
  // 1) Respect .graphqlrc.yml if present
  const fromRc = readRcSchemas(projectRoot).map((p) =>
    isHttpUrl(p) ? p : toPosix(path.isAbsolute(p) ? p : path.join(projectRoot, p)),
  );
  if (fromRc.length > 0) return fromRc;

  // 2) Fallback defaults
  const schemaLocalDefault = toPosix(path.join(sourceRootAbs, 'gql', 'schema.graphql'));
  const defaults: string[] = [];
  if (fs.existsSync(schemaLocalDefault)) defaults.push(schemaLocalDefault);
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

        // Sync schemas based on vite config option localSchemaMap; keep other .graphqlrc.yml entries intact
        try {
          const parsed = parseGraphqlRc(projectRoot);
          const map = options.localSchemaMap ?? {};
          const urls = Object.keys(map).filter((k) => isHttpUrl(k));
          if (urls.length > 0) {
            const generatedFiles: string[] = [];
            for (const url of urls) {
              const rel = toPosix(map[url]);
              const abs = toPosix(path.isAbsolute(rel) ? rel : path.join(projectRoot, rel));
              fs.mkdirSync(path.dirname(abs), { recursive: true });
              const sdl = await fetchRemoteSchemaSdl(url);
              fs.writeFileSync(abs, sdl, 'utf8');
              generatedFiles.push(toPosix(path.relative(projectRoot, abs)) || rel);
              console.info(`[gqlgen] Downloaded schema from ${url} -> ${rel}`);
            }
            if (generatedFiles.length > 0) {
              const toAdd = Array.from(new Set([...generatedFiles, ...urls]));
              if (parsed) {
                const nextEntries = Array.from(new Set([...toAdd, ...parsed.schemaEntries]));
                writeGraphqlRcSchemaBlock(projectRoot, parsed, nextEntries);
              } else {
                writeNewGraphqlRcWithSchemaEntries(projectRoot, toAdd);
              }
              console.info('[gqlgen] Updated .graphqlrc.yml: ensured mapped schema files and URLs are listed.');
            }
          }
          // Ensure required documents globs are present
          const parsedAfter = parseGraphqlRc(projectRoot);
          if (parsedAfter) {
            const existingDocsNormalized = new Set(
              (parsedAfter.documentsEntries || []).map((e) => stripSurroundingQuotes(e)),
            );
            const missing = REQUIRED_DOCUMENTS_GLOBS_UNQUOTED.filter((u) => !existingDocsNormalized.has(u));
            if (missing.length > 0) {
              const nextDocumentsEntries = [...(parsedAfter.documentsEntries || [])];
              for (const unq of missing) {
                const writable = REQUIRED_DOCUMENTS_MAP[unq] || unq;
                nextDocumentsEntries.push(writable);
              }
              writeGraphqlRcDocumentsBlock(projectRoot, parsedAfter, Array.from(new Set(nextDocumentsEntries)));
              console.info('[gqlgen] Updated .graphqlrc.yml: ensured required documents globs exist.');
            }
          }
        } catch (e) {
          console.warn('[gqlgen] Unable to update .graphqlrc.yml based on localSchemaMap:', e);
        }

        const schemaEntries = resolveSchemas(projectRoot, sourceRootAbs);
        // Build whitelist of allowed .graphql paths (schema entries that are local files)
        const allowedGraphqlPathsAbs = new Set(
          schemaEntries
            .filter((s) => !isHttpUrl(s))
            .map((s) => (path.isAbsolute(s) ? toPosix(s) : toPosix(path.join(projectRoot, s)))),
        );
        // Enforce no .graphql documents (only .gql allowed) except whitelisted schema files
        assertNoGraphqlDocuments(sourceRootAbs, allowedGraphqlPathsAbs);

        if (schemaEntries.length === 0) {
          console.error('[gqlgen] No GraphQL schema configured. Please specify schema files/URLs via plugin options or .graphqlrc.yml.');
          return;
        }

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
