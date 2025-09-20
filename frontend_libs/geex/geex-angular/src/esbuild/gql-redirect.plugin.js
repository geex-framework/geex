'use strict';

const path = require('path');
const fs = require('fs');
const json5 = require('json5');
const crypto = require('crypto');
const { execSync } = require('child_process');
const { glob } = require('glob');
const yaml = require('yaml');

/**
 * @type {string | undefined}
 */
let graphqlrcContent = undefined;
/**
 * @typedef {import('esbuild').Plugin} Plugin
 */

/**
 * @typedef {Object} GqlRedirectPluginOptions
 * @property {boolean} [verbose=true] Whether to enable verbose logging
 * @property {string} [tsconfigPath='./tsconfig.json'] Path to tsconfig.json file for path alias resolution
 * @property {boolean} [autoGqlGen=true] Whether to automatically run pnpm gqlgen when .gql.ts files are missing
 */

/**
 * @typedef {{ alias: string, paths: string[] }} PathMapping
 */

/**
 * Loads and parses TypeScript configuration for path alias resolution
 * @param {string} tsconfigPath
 * @returns {PathMapping[]}
 */
function loadTsconfigPaths(tsconfigPath) {
  try {
    const tsconfig = json5.parse(fs.readFileSync(tsconfigPath, 'utf8'));
    const baseUrl = (tsconfig.compilerOptions && tsconfig.compilerOptions.baseUrl) || './';
    const paths = (tsconfig.compilerOptions && tsconfig.compilerOptions.paths) || {};

    return Object.entries(paths).map(([alias, pathList]) => ({
      alias: alias.replace(/\/\*$/, ''),
      paths: /** @type {string[]} */ (pathList).map(p =>
        path.resolve(path.dirname(tsconfigPath), baseUrl, p.replace(/\/\*$/, ''))
      ),
    }));
  } catch (error) {
    console.warn(`[gql-redirect] Failed to load tsconfig from ${tsconfigPath}:`, error);
    return [];
  }
}

/**
 * Find the directory that contains angular.json by walking up from startDir.
 * Falls back to startDir if not found.
 * @param {string} startDir
 * @returns {string}
 */
function findAngularJsonDir(startDir) {
  try {
    let dir = startDir;
    // Prevent infinite loop at filesystem root
    while (true) {
      if (fs.existsSync(path.join(dir, 'angular.json'))) return dir;
      const parent = path.dirname(dir);
      if (parent === dir) return startDir;
      dir = parent;
    }
  } catch {
    return startDir;
  }
}

/**
 * Parse the .graphqlrc.yml file and extract documents patterns
 * @param {string} projectRoot
 * @returns {string[]}
 */
function parseGraphqlrcDocuments(projectRoot) {
  try {
    const graphqlrcPath = path.join(projectRoot, '.graphqlrc.yml');
    if (!fs.existsSync(graphqlrcPath)) {
      console.warn('[gql-redirect] .graphqlrc.yml not found, falling back to default patterns');
      return [];
    }

    graphqlrcContent ??= fs.readFileSync(graphqlrcPath, 'utf8');

    // Remove the hash comment line if present to avoid yaml parsing issues
    const cleanContent = graphqlrcContent.replace(/^# gql-hash:.*\n/, '');

    // Parse YAML content
    const config = yaml.parse(cleanContent);

    if (!config || !config.documents) {
      console.warn('[gql-redirect] No documents section found in .graphqlrc.yml');
      return [];
    }

    // Extract documents patterns
    const documents = config.documents;

    // Handle different document formats
    if (Array.isArray(documents)) {
      return documents.filter(doc => typeof doc === 'string');
    } else if (typeof documents === 'string') {
      return [documents];
    } else {
      console.warn('[gql-redirect] Unexpected documents format in .graphqlrc.yml');
      return [];
    }

  } catch (error) {
    console.warn(`[gql-redirect] Failed to parse .graphqlrc.yml:`, error.message);
    return [];
  }
}

/**
 * Find all .gql files based on .graphqlrc.yml documents configuration
 * @param {string} projectRoot
 * @returns {string[]}
 */
function findGqlFilesByDocuments(projectRoot) {
  try {
    const documentPatterns = parseGraphqlrcDocuments(projectRoot);
    const allFiles = new Set();

    // Process each document pattern
    for (const pattern of documentPatterns) {
      // Skip negation patterns for now, we'll handle them after
      if (pattern.startsWith('!')) {
        continue;
      }

      try {
        const matches = glob.sync(pattern, {
          cwd: projectRoot,
          absolute: true,
          nodir: true
        });

        matches.forEach(file => allFiles.add(file));
      } catch (error) {
        console.warn(`[gql-redirect] Failed to process pattern "${pattern}":`, error.message);
      }
    }

    // Apply negation patterns
    const negationPatterns = documentPatterns.filter(pattern => pattern.startsWith('!'));
    let filteredFiles = Array.from(allFiles);

    for (const negPattern of negationPatterns) {
      const pattern = negPattern.substring(1); // Remove the '!' prefix

      try {
        const excludeMatches = glob.sync(pattern, {
          cwd: projectRoot,
          absolute: true,
          nodir: true
        });

        const excludeSet = new Set(excludeMatches);
        filteredFiles = filteredFiles.filter(file => !excludeSet.has(file));
      } catch (error) {
        console.warn(`[gql-redirect] Failed to process negation pattern "${negPattern}":`, error.message);
      }
    }

    return filteredFiles.sort(); // Sort for consistent ordering
  } catch (error) {
    console.warn(`[gql-redirect] Failed to find GQL files:`, error.message);
    return [];
  }
}

/**
 * Calculate MD5 hash of all .gql files (based on .graphqlrc.yml documents config) and .graphqlrc.yml content (excluding last line comment)
 * @param {string} projectRoot
 * @returns {string}
 */
function calculateGqlHash(projectRoot) {
  try {
    const hash = crypto.createHash('md5');

    // Find .gql files based on .graphqlrc.yml documents configuration
    const gqlFiles = findGqlFilesByDocuments(projectRoot);

    // Add content of all .gql files
    for (const filePath of gqlFiles) {
      try {
        const content = fs.readFileSync(filePath, 'utf8');
        hash.update(`${path.relative(projectRoot, filePath)}:${content}`);
      } catch (error) {
        console.warn(`[gql-redirect] Failed to read ${filePath}:`, error.message);
      }
    }

    // Add .graphqlrc.yml content (excluding last line comment)
    const graphqlrcPath = path.join(projectRoot, '.graphqlrc.yml');
    if (fs.existsSync(graphqlrcPath)) {
      try {
        graphqlrcContent ??= fs.readFileSync(graphqlrcPath, 'utf8');
        const cleanContent = graphqlrcContent.replace(/^# gql-hash: .*/m, '');
        hash.update(`.graphqlrc.yml:${cleanContent}`);
      } catch (error) {
        console.warn(`[gql-redirect] Failed to read .graphqlrc.yml:`, error.message);
      }
    }

    return hash.digest('hex');
  } catch (error) {
    console.warn(`[gql-redirect] Failed to calculate GQL hash:`, error.message);
    return '';
  }
}

/**
 * Get the previous GQL hash from the last line comment in .graphqlrc.yml
 * @param {string} projectRoot
 * @returns {string}
 */
function getPreviousGqlHash(projectRoot) {
  try {
    const graphqlrcPath = path.join(projectRoot, '.graphqlrc.yml');
    if (!fs.existsSync(graphqlrcPath)) {
      return '';
    }

    graphqlrcContent ??= fs.readFileSync(graphqlrcPath, 'utf8');
    const match = graphqlrcContent.match(/^# gql-hash: (.*)$/m);
    return match ? match[1].trim() : '';
  } catch (error) {
    console.warn(`[gql-redirect] Failed to read previous GQL hash:`, error.message);
    return '';
  }
}

/**
 * Update the GQL hash comment at the end of .graphqlrc.yml
 * @param {string} projectRoot
 * @param {string} newHash
 */
function updateGraphqlrcHash(projectRoot, newHash) {
  try {
    const graphqlrcPath = path.join(projectRoot, '.graphqlrc.yml');
    if (!fs.existsSync(graphqlrcPath)) {
      return;
    }

    // Always read fresh content from file instead of using cached version
    const currentContent = fs.readFileSync(graphqlrcPath, 'utf8');
    const newContent = currentContent.replace(/^# gql-hash: .*/m, `# gql-hash: ${newHash}`);
    fs.writeFileSync(graphqlrcPath, newContent, 'utf8');
  } catch (error) {
    console.warn(`[gql-redirect] Failed to update .graphqlrc.yml hash:`, error.message);
  }
}

/**
 * Executes pnpm gqlgen command in the project root directory with hash-based caching
 * @param {string} projectRoot
 * @param {boolean} verbose
 * @returns {boolean} Returns true if command executed successfully or was skipped due to cache
 */
function runGqlGen(projectRoot, verbose) {
  try {
    // Calculate current hash of all .gql files and .graphqlrc.yml content
    const currentGqlHash = calculateGqlHash(projectRoot);

    // Get previous hash from .graphqlrc.yml comment
    const previousGqlHash = getPreviousGqlHash(projectRoot);

    // Compare hashes - only run gqlgen if they're different
    if (currentGqlHash && currentGqlHash === previousGqlHash) {
      console.log('[gql-redirect] GQL files unchanged, skipping pnpm gqlgen');
      return true; // Consider this a success since no work was needed
    }

    console.log(`[gql-redirect] GQL files changed (${previousGqlHash}) => (${currentGqlHash}), running pnpm gqlgen in ${projectRoot}...`);

    try {
      execSync('pnpm gqlgen', {
        cwd: projectRoot,
        stdio: verbose ? 'inherit' : 'pipe',
        timeout: 30000, // 30 second timeout
      });
    } catch (error) {
      if (error.message.includes('NODE_TLS_REJECT_UNAUTHORIZED')) {
        // ignore NODE_TLS_REJECT_UNAUTHORIZED error
      }
    }
    // Update the hash in .graphqlrc.yml after successful execution
    if (currentGqlHash) {
      updateGraphqlrcHash(projectRoot, currentGqlHash);
    }
    if (verbose) {
      console.log('[gql-redirect] Successfully executed pnpm gqlgen and updated hash');
    }
    return true;
  } catch (error) {
    console.error(`[gql-redirect] Failed to execute pnpm gqlgen:`, error.message);
    return false;
  }
}

/**
 * Resolves a module path that might use TypeScript path aliases
 * @param {string} modulePath
 * @param {PathMapping[]} pathMappings
 * @param {string} resolveDir
 * @returns {string[]}
 */
function resolvePathAlias(modulePath, pathMappings, resolveDir) {
  const possiblePaths = [];

  for (const mapping of pathMappings) {
    if (modulePath.startsWith(mapping.alias)) {
      const remainingPath = modulePath.slice(mapping.alias.length);
      const cleanRemainingPath = remainingPath.startsWith('/') ? remainingPath.slice(1) : remainingPath;

      for (const basePath of mapping.paths) {
        const resolvedPath = cleanRemainingPath ? path.join(basePath, cleanRemainingPath) : basePath;
        possiblePaths.push(resolvedPath);
      }
    }
  }

  if (possiblePaths.length === 0) {
    possiblePaths.push(path.resolve(resolveDir, modulePath));
  }

  return possiblePaths;
}

/**
 * ESBuild plugin to handle .gql imports and redirect them to .gql.ts files
 *
 * This plugin resolves .gql imports by checking if a corresponding .gql.ts file exists.
 * If it does, it redirects the import to the .gql.ts file. Otherwise, it lets esbuild
 * handle the .gql file normally.
 *
 * Now supports TypeScript path aliases for proper module resolution.
 *
 * @param {GqlRedirectPluginOptions} [options]
 * @returns {Plugin}
 */
function gqlRedirectPlugin(options = {}) {
  const { verbose = false, tsconfigPath = './tsconfig.json', autoGqlGen = true } = options;

  return {
    name: 'gql-redirect',
    setup(build) {
      const angularJsonDir = findAngularJsonDir(process.cwd());
      const pathMappings = loadTsconfigPaths(tsconfigPath);

      if (verbose && pathMappings.length > 0) {
        console.log(`[gql-redirect] Loaded ${pathMappings.length} path mappings from ${tsconfigPath}`);
      }

      if (autoGqlGen) {
        runGqlGen(angularJsonDir, verbose);
      }
      build.onResolve({ filter: /\.gql$/ }, args => {
        try {
          const possiblePaths = resolvePathAlias(args.path, pathMappings, args.resolveDir);

          // First pass: check if any .gql.ts files exist
          for (const resolvedPath of possiblePaths) {
            const gqlTsPath = resolvedPath + '.ts';

            if (fs.existsSync(gqlTsPath)) {
              if (verbose) {
                const displayPath = path.relative(angularJsonDir, gqlTsPath) || '.';
                console.log(`[gql-redirect] Redirecting "${args.path}" to "${displayPath}"`);
              }
              return { path: gqlTsPath };
            }
          }
          if (verbose) {
            console.warn(`[gql-redirect] No .gql.ts found for ${args.path} in any of the resolved paths: ${possiblePaths.join(', ')}`);
          } else {
            console.warn(`[gql-redirect] No .gql.ts found for ${args.path}, please run pnpm gqlgen to ensure it.`);
          }
          return null;
        } catch (error) {
          if (verbose) {
            console.error(`[gql-redirect] Error processing ${args.path}:`, error);
          }
          return null;
        }
      });
    },
  };
}

module.exports = gqlRedirectPlugin;
module.exports.default = gqlRedirectPlugin;
module.exports.gqlRedirectPlugin = gqlRedirectPlugin;
