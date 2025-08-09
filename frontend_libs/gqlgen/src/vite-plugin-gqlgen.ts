import type { Plugin } from 'vite';
import path from 'node:path';
import fs from 'node:fs';
import * as YAML from 'yaml';
import { generate } from '@graphql-codegen/cli';
import { buildClientSchema, getIntrospectionQuery, printSchema } from 'graphql';
import { createInterface } from 'node:readline';

export interface GqlgenOptions {
  /** Where to emit shared types. Defaults to `${sourceRoot}/graphql` */
  sharedTypesDir?: string;
  /** Custom scalar mappings. Example: { URL: 'string' } */
  scalars?: Record<string, string>;
  /** GraphQL document file extension. Defaults to 'gql' */
  gqlDocFileExtension?: 'gql' | 'graphql';
}

interface PluginState {
  projectRoot: string;
  sourceRoot: string;
  extensionMap: Map<string, string>;
}

// Default scalar mappings
const DEFAULT_SCALARS: Record<string, string> = {
  ChinesePhoneNumber: 'string',
  DateTime: 'Date',
  Decimal: 'number',
  Long: 'BigInt',
  ObjectId: 'string',
  MimeType: 'string',
};

// Path utilities
const toPosix = (p: string): string => p.replace(/\\/g, '/');
const ensureDotPrefix = (p: string): string => p.startsWith('.') ? p : `./${p}`;
const isHttpUrl = (value: string): boolean => /^(https?:)?\/\//i.test(value);

// User interaction utilities
function askUserConfirmation(message: string): Promise<boolean> {
  return new Promise((resolve) => {
    const rl = createInterface({
      input: process.stdin,
      output: process.stdout
    });

    rl.question(`${message} (y/N): `, (answer) => {
      rl.close();
      resolve(answer.toLowerCase().trim() === 'y' || answer.toLowerCase().trim() === 'yes');
    });
  });
}

// Project structure helpers
function resolveSourceRoot(projectRoot: string): string {
  const tsconfigPath = path.join(projectRoot, 'tsconfig.json');
  try {
    if (fs.existsSync(tsconfigPath)) {
      const config = JSON.parse(fs.readFileSync(tsconfigPath, 'utf8'));
      const { compilerOptions = {}, include = [] } = config;
      
      // Check explicit root directories
      const rootDir = compilerOptions.rootDir || compilerOptions.sourceRoot;
      if (rootDir) {
        return toPosix(path.isAbsolute(rootDir) ? rootDir : path.join(projectRoot, rootDir));
      }
      
      // Check include patterns for src
      if (include.some((inc: string) => inc.includes('src'))) {
        return toPosix(path.join(projectRoot, 'src'));
      }
    }
  } catch {
    // Fallback
  }
  return toPosix(path.join(projectRoot, 'src'));
}

function buildDocumentGlobs(sourceRootRel: string, extension: string): string[] {
  const base = sourceRootRel === '.' || sourceRootRel === '' ? '' : `${sourceRootRel}/`;
  
  return [
    `${base}**/*.{ts,tsx,vue,${extension}}`,
    `!${base}**/*.{test,spec}.*`,
    `!${base}**/*.{gql,graphql}.ts`,
    `!${base}**/node_modules/**`,
  ];
}

function computeBaseTypesImportPath(sourceRootAbs: string, sharedTypesDirAbs: string, gqlDocExtension: string): string {
  const schemaFileName = gqlDocExtension === 'graphql' ? 'schema.graphql.ts' : 'schema.gql.ts';
  const relativePath = toPosix(
    path.relative(sourceRootAbs, path.join(sharedTypesDirAbs, schemaFileName))
  );
  return ensureDotPrefix(relativePath);
}

// GraphQL RC configuration helpers
function loadGraphqlRc(projectRoot: string): { rcPath: string; doc: any } {
  const rcPath = path.join(projectRoot, '.graphqlrc.yml');
  let doc;
  
  if (fs.existsSync(rcPath)) {
    const content = fs.readFileSync(rcPath, 'utf8');
    doc = YAML.parseDocument(content);
    if (!doc.contents) doc.contents = YAML.createNode({});
  } else {
    doc = YAML.parseDocument('');
    doc.contents = YAML.createNode({});
  }
  
  return { rcPath, doc };
}

function yamlToStringArray(value: unknown): string[] {
  if (!value) return [];
  if (Array.isArray(value)) return value.map(String);
  if (typeof value === 'string') return [value];
  
  const asAny = value as any;
  if (asAny?.items?.length) {
    return asAny.items.map((item: any) => String(item?.value ?? item)).filter(Boolean);
  }
  if (asAny?.value) return [String(asAny.value)];
  
  return [];
}

function updateGraphqlRc(projectRoot: string, updater: (doc: any) => void): void {
  const { rcPath, doc } = loadGraphqlRc(projectRoot);
  updater(doc);
  fs.writeFileSync(rcPath, doc.toString(), 'utf8');
}

// Schema fetching
async function fetchRemoteSchema(url: string): Promise<string> {
  const introspectionQuery = getIntrospectionQuery();
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ query: introspectionQuery }),
  });
  
  if (!response.ok) {
    throw `Failed to fetch schema from ${url}: ${response.status}`;
  }
  
  const result = await response.json();
  if (result.errors) {
    throw `Schema introspection failed: ${JSON.stringify(result.errors)}`;
  }
  
  return printSchema(buildClientSchema(result.data));
}

// Schema resolution
function resolveSchemas(projectRoot: string): string[] {
  try {
    const { doc } = loadGraphqlRc(projectRoot);
    const schemas = yamlToStringArray(doc.get('schema'));
    
    if (schemas.length > 0) {
      return schemas.map(p => 
        isHttpUrl(p) ? p : toPosix(path.isAbsolute(p) ? p : path.join(projectRoot, p))
      );
    }
  } catch {
    // Continue to fallback
  }

  // Check schemas directory
  const schemasDir = path.join(projectRoot, 'schemas');
  if (fs.existsSync(schemasDir)) {
    const schemaFiles = fs.readdirSync(schemasDir)
      .filter(file => file.endsWith('.graphql') || file.endsWith('.gql'))
      .map(file => toPosix(path.join(schemasDir, file)));
    if (schemaFiles.length > 0) return schemaFiles;
  }

  return [];
}

// File extension normalization  
async function createExtensionMap(sourceRoot: string, options: GqlgenOptions, sharedTypesDir: string): Promise<Map<string, string>> {
  const extensionMap = new Map<string, string>();
  const targetExt = options.gqlDocFileExtension || 'gql';
  const sourceExt = targetExt === 'gql' ? 'graphql' : 'gql';
  const filesToRename: Array<{ from: string; to: string; fromTs: string; toTs: string }> = [];
  
  try {
    const findFiles = (dir: string): void => {
      const entries = fs.readdirSync(dir, { withFileTypes: true });
      for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        
        if (entry.isDirectory() && entry.name !== 'node_modules') {
          findFiles(fullPath);
        } else if (entry.isFile() && entry.name.endsWith(`.${sourceExt}`)) {
          const targetPath = fullPath.replace(`.${sourceExt}`, `.${targetExt}`);
          const fromTsPath = `${fullPath}.ts`;
          const toTsPath = `${targetPath}.ts`;
          extensionMap.set(toPosix(fullPath), toPosix(targetPath));
          filesToRename.push({ from: fullPath, to: targetPath, fromTs: fromTsPath, toTs: toTsPath });
        }
      }
    };
    
    findFiles(sourceRoot);
    
    // Also check for old schema file in shared types directory
    const oldSchemaFileName = sourceExt === 'gql' ? 'schema.gql.ts' : 'schema.graphql.ts';
    const newSchemaFileName = targetExt === 'gql' ? 'schema.gql.ts' : 'schema.graphql.ts';
    const oldSchemaPath = path.join(sharedTypesDir, oldSchemaFileName);
    const newSchemaPath = path.join(sharedTypesDir, newSchemaFileName);
    
    if (fs.existsSync(oldSchemaPath) && oldSchemaFileName !== newSchemaFileName) {
      filesToRename.push({ 
        from: oldSchemaPath, 
        to: newSchemaPath, 
        fromTs: oldSchemaPath, // Schema file is already a .ts file
        toTs: newSchemaPath 
      });
    }
    
    if (filesToRename.length > 0) {
      console.info(`\n[gqlgen] Found ${filesToRename.length} files to normalize to .${targetExt} extension:`);
      filesToRename.forEach(({ from, to, fromTs, toTs }) => {
        console.info(`  ${path.relative(process.cwd(), from)} → ${path.relative(process.cwd(), to)}`);
        if (fs.existsSync(fromTs) && fromTs !== from) {
          console.info(`  ${path.relative(process.cwd(), fromTs)} → ${path.relative(process.cwd(), toTs)}`);
        }
      });
      
      const confirmed = await askUserConfirmation(
        `\n[gqlgen] Do you want to proceed with normalization? This will:\n` +
        `  - Rename ${filesToRename.length} files to use .${targetExt} extension\n` +
        `  - Also rename corresponding TypeScript files`
      );
      
      if (confirmed) {
        let successCount = 0;
        for (const { from, to, fromTs, toTs } of filesToRename) {
          try {
            // Check if this is a schema file (fromTs === from means it's already a .ts file)
            if (fromTs === from) {
              // This is a schema file, just rename it
              const content = fs.readFileSync(from, 'utf8');
              fs.writeFileSync(to, content, 'utf8');
              fs.rmSync(from);
              console.info(`[gqlgen] ✓ Normalized: ${path.relative(process.cwd(), from)}`);
            } else {
              // This is a GraphQL document file
              const content = fs.readFileSync(from, 'utf8');
              fs.writeFileSync(to, content, 'utf8');
              fs.rmSync(from);
              
              // Rename corresponding TypeScript file if it exists
              if (fs.existsSync(fromTs)) {
                const tsContent = fs.readFileSync(fromTs, 'utf8');
                fs.writeFileSync(toTs, tsContent, 'utf8');
                fs.rmSync(fromTs);
                console.info(`[gqlgen] ✓ Normalized: ${path.relative(process.cwd(), from)} and ${path.relative(process.cwd(), fromTs)}`);
              } else {
                console.info(`[gqlgen] ✓ Normalized: ${path.relative(process.cwd(), from)}`);
              }
            }
            
            successCount++;
          } catch (err) {
            console.warn(`[gqlgen] ✗ Failed to normalize ${path.relative(process.cwd(), from)}:`, err);
          }
        }
        console.info(`\n[gqlgen] Successfully normalized ${successCount}/${filesToRename.length} files`);
      } else {
        console.info('[gqlgen] File normalization cancelled by user');
        return new Map(); // Return empty map if cancelled
      }
    }
  } catch (err) {
    console.warn('[gqlgen] Error creating extension map:', err);
  }
  
  return extensionMap;
}

// Code generation
async function generateCode(
  schemas: string[], 
  sourceRoot: string, 
  sharedTypesDir: string, 
  baseTypesImportPath: string, 
  scalars: Record<string, string>, 
  options: GqlgenOptions,
  projectRoot: string
) {
  const gqlDocExtension = options.gqlDocFileExtension || 'gql';
  const documents = buildDocumentGlobs(path.relative(projectRoot, sourceRoot) || '.', gqlDocExtension);
  
  const baseConfig = {
    schema: schemas,
    overwrite: true,
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
    }
  };

  const outputExtension = gqlDocExtension === 'graphql' ? '.graphql.ts' : '.gql.ts';
  const schemaFileName = gqlDocExtension === 'graphql' ? 'schema.graphql.ts' : 'schema.gql.ts';

  await generate({
    ...baseConfig,
    generates: {
      [path.join(sharedTypesDir, schemaFileName)]: {
        plugins: ['typescript'],
      },
      [sourceRoot]: {
        preset: 'near-operation-file',
        presetConfig: {
          extension: outputExtension,
          baseTypesPath: baseTypesImportPath,
        },
        plugins: ['typescript-operations', 'typed-document-node'],
      },
      [path.join(sharedTypesDir, 'apollo-helpers.g.ts')]: {
        plugins: ['typescript-apollo-client-helpers'],
      },
    },
  } as any, true);
}

const gqlgen = (options: GqlgenOptions): Plugin => {
  const state: PluginState = { 
    projectRoot: '', 
    sourceRoot: '', 
    extensionMap: new Map() 
  };

  return {
    name: 'vite-plugin-gqlgen',
    enforce: 'pre',

    configResolved(config) {
      state.projectRoot = toPosix(config.root || process.cwd());
      state.sourceRoot = resolveSourceRoot(state.projectRoot);
    },

    async buildStart() {
      const { projectRoot, sourceRoot } = state;
      const sharedTypesDir = options.sharedTypesDir
        ? path.isAbsolute(options.sharedTypesDir)
          ? options.sharedTypesDir
          : path.join(projectRoot, options.sharedTypesDir)
        : path.join(sourceRoot, 'graphql');

      // Handle extension normalization
      state.extensionMap = await createExtensionMap(sourceRoot, options, sharedTypesDir);

      // Setup paths and configuration
      const gqlDocExtension = options.gqlDocFileExtension || 'gql';
      const sourceRootRel = path.relative(projectRoot, sourceRoot) || '.';
      const baseTypesImportPath = computeBaseTypesImportPath(sourceRoot, sharedTypesDir, gqlDocExtension);
      const scalars = { ...DEFAULT_SCALARS, ...(options.scalars || {}) };

      // Update document globs in .graphqlrc.yml
      const documentGlobs = buildDocumentGlobs(sourceRootRel, gqlDocExtension);
      updateGraphqlRc(projectRoot, (doc) => {
        const existing = yamlToStringArray(doc.get('documents'));
        doc.set('documents', [...new Set([...existing, ...documentGlobs])]);
      });

      // Resolve schema sources
      const schemas = resolveSchemas(projectRoot);
      if (schemas.length === 0) {
        throw '[gqlgen] No GraphQL schema configured. Please add schema files or URLs.';
      }

      // Generate TypeScript from GraphQL
      await generateCode(schemas, sourceRoot, sharedTypesDir, baseTypesImportPath, scalars, options, projectRoot);
    },

    resolveId(id, importer) {
      if (!id.endsWith('.gql') && !id.endsWith('.graphql')) {
        return null;
      }

      // Resolve the GraphQL file path
      const baseDir = importer ? path.dirname(importer.split('?')[0]) : state.projectRoot;
      const resolvedPath = path.resolve(baseDir, id);
      
      // Check for normalized extension mapping
      const normalizedPath = state.extensionMap.get(toPosix(resolvedPath));
      if (normalizedPath) {
        const gqlDocExtension = options.gqlDocFileExtension || 'gql';
        const ext = gqlDocExtension === 'graphql' ? '.graphql.ts' : '.gql.ts';
        const tsFile = normalizedPath.replace(/\.(gql|graphql)$/, ext);
        if (fs.existsSync(tsFile)) {
          return toPosix(tsFile);
        }
      }
      
      // Standard resolution - look for generated TypeScript file
      const gqlDocExtension = options.gqlDocFileExtension || 'gql';
      const ext = gqlDocExtension === 'graphql' ? '.graphql.ts' : '.gql.ts';
      const tsFile = resolvedPath.replace(/\.(gql|graphql)$/, ext);
      
      if (fs.existsSync(tsFile)) {
        return toPosix(tsFile);
      }
      
      throw `GraphQL file "${id}" has no generated TypeScript file. Run the build to generate types.`;
    },
  };
};

export default gqlgen;
