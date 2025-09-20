import {
  __commonJS,
  __require
} from "../chunk-EBO3CZXG.mjs";

// src/esbuild/gql-redirect.plugin.js
var require_gql_redirect_plugin = __commonJS({
  "src/esbuild/gql-redirect.plugin.js"(exports, module) {
    var path = __require("path");
    var fs = __require("fs");
    var json5 = __require("json5");
    var crypto = __require("crypto");
    var { execSync } = __require("child_process");
    var { glob } = __require("glob");
    var yaml = __require("yaml");
    var graphqlrcContent = void 0;
    function loadTsconfigPaths(tsconfigPath) {
      try {
        const tsconfig = json5.parse(fs.readFileSync(tsconfigPath, "utf8"));
        const baseUrl = tsconfig.compilerOptions && tsconfig.compilerOptions.baseUrl || "./";
        const paths = tsconfig.compilerOptions && tsconfig.compilerOptions.paths || {};
        return Object.entries(paths).map(([alias, pathList]) => ({
          alias: alias.replace(/\/\*$/, ""),
          paths: (
            /** @type {string[]} */
            pathList.map(
              (p) => path.resolve(path.dirname(tsconfigPath), baseUrl, p.replace(/\/\*$/, ""))
            )
          )
        }));
      } catch (error) {
        console.warn(`[gql-redirect] Failed to load tsconfig from ${tsconfigPath}:`, error);
        return [];
      }
    }
    function findAngularJsonDir(startDir) {
      try {
        let dir = startDir;
        while (true) {
          if (fs.existsSync(path.join(dir, "angular.json"))) return dir;
          const parent = path.dirname(dir);
          if (parent === dir) return startDir;
          dir = parent;
        }
      } catch {
        return startDir;
      }
    }
    function parseGraphqlrcDocuments(projectRoot) {
      try {
        const graphqlrcPath = path.join(projectRoot, ".graphqlrc.yml");
        if (!fs.existsSync(graphqlrcPath)) {
          console.warn("[gql-redirect] .graphqlrc.yml not found, falling back to default patterns");
          return [];
        }
        graphqlrcContent ?? (graphqlrcContent = fs.readFileSync(graphqlrcPath, "utf8"));
        const cleanContent = graphqlrcContent.replace(/^# gql-hash:.*\n/, "");
        const config = yaml.parse(cleanContent);
        if (!config || !config.documents) {
          console.warn("[gql-redirect] No documents section found in .graphqlrc.yml");
          return [];
        }
        const documents = config.documents;
        if (Array.isArray(documents)) {
          return documents.filter((doc) => typeof doc === "string");
        } else if (typeof documents === "string") {
          return [documents];
        } else {
          console.warn("[gql-redirect] Unexpected documents format in .graphqlrc.yml");
          return [];
        }
      } catch (error) {
        console.warn(`[gql-redirect] Failed to parse .graphqlrc.yml:`, error.message);
        return [];
      }
    }
    function findGqlFilesByDocuments(projectRoot) {
      try {
        const documentPatterns = parseGraphqlrcDocuments(projectRoot);
        const allFiles = /* @__PURE__ */ new Set();
        for (const pattern of documentPatterns) {
          if (pattern.startsWith("!")) {
            continue;
          }
          try {
            const matches = glob.sync(pattern, {
              cwd: projectRoot,
              absolute: true,
              nodir: true
            });
            matches.forEach((file) => allFiles.add(file));
          } catch (error) {
            console.warn(`[gql-redirect] Failed to process pattern "${pattern}":`, error.message);
          }
        }
        const negationPatterns = documentPatterns.filter((pattern) => pattern.startsWith("!"));
        let filteredFiles = Array.from(allFiles);
        for (const negPattern of negationPatterns) {
          const pattern = negPattern.substring(1);
          try {
            const excludeMatches = glob.sync(pattern, {
              cwd: projectRoot,
              absolute: true,
              nodir: true
            });
            const excludeSet = new Set(excludeMatches);
            filteredFiles = filteredFiles.filter((file) => !excludeSet.has(file));
          } catch (error) {
            console.warn(`[gql-redirect] Failed to process negation pattern "${negPattern}":`, error.message);
          }
        }
        return filteredFiles.sort();
      } catch (error) {
        console.warn(`[gql-redirect] Failed to find GQL files:`, error.message);
        return [];
      }
    }
    function calculateGqlHash(projectRoot) {
      try {
        const hash = crypto.createHash("md5");
        const gqlFiles = findGqlFilesByDocuments(projectRoot);
        for (const filePath of gqlFiles) {
          try {
            const content = fs.readFileSync(filePath, "utf8");
            hash.update(`${path.relative(projectRoot, filePath)}:${content}`);
          } catch (error) {
            console.warn(`[gql-redirect] Failed to read ${filePath}:`, error.message);
          }
        }
        const graphqlrcPath = path.join(projectRoot, ".graphqlrc.yml");
        if (fs.existsSync(graphqlrcPath)) {
          try {
            graphqlrcContent ?? (graphqlrcContent = fs.readFileSync(graphqlrcPath, "utf8"));
            const cleanContent = graphqlrcContent.replace(/^# gql-hash: .*/m, "");
            hash.update(`.graphqlrc.yml:${cleanContent}`);
          } catch (error) {
            console.warn(`[gql-redirect] Failed to read .graphqlrc.yml:`, error.message);
          }
        }
        return hash.digest("hex");
      } catch (error) {
        console.warn(`[gql-redirect] Failed to calculate GQL hash:`, error.message);
        return "";
      }
    }
    function getPreviousGqlHash(projectRoot) {
      try {
        const graphqlrcPath = path.join(projectRoot, ".graphqlrc.yml");
        if (!fs.existsSync(graphqlrcPath)) {
          return "";
        }
        graphqlrcContent ?? (graphqlrcContent = fs.readFileSync(graphqlrcPath, "utf8"));
        const match = graphqlrcContent.match(/^# gql-hash: (.*)$/m);
        return match ? match[1].trim() : "";
      } catch (error) {
        console.warn(`[gql-redirect] Failed to read previous GQL hash:`, error.message);
        return "";
      }
    }
    function updateGraphqlrcHash(projectRoot, newHash) {
      try {
        const graphqlrcPath = path.join(projectRoot, ".graphqlrc.yml");
        if (!fs.existsSync(graphqlrcPath)) {
          return;
        }
        const currentContent = fs.readFileSync(graphqlrcPath, "utf8");
        const newContent = currentContent.replace(/^# gql-hash: .*/m, `# gql-hash: ${newHash}`);
        fs.writeFileSync(graphqlrcPath, newContent, "utf8");
      } catch (error) {
        console.warn(`[gql-redirect] Failed to update .graphqlrc.yml hash:`, error.message);
      }
    }
    function runGqlGen(projectRoot, verbose) {
      try {
        const currentGqlHash = calculateGqlHash(projectRoot);
        const previousGqlHash = getPreviousGqlHash(projectRoot);
        if (currentGqlHash && currentGqlHash === previousGqlHash) {
          console.log("[gql-redirect] GQL files unchanged, skipping pnpm gqlgen");
          return true;
        }
        console.log(`[gql-redirect] GQL files changed (${previousGqlHash}) => (${currentGqlHash}), running pnpm gqlgen in ${projectRoot}...`);
        try {
          execSync("pnpm gqlgen", {
            cwd: projectRoot,
            stdio: verbose ? "inherit" : "pipe",
            timeout: 3e4
            // 30 second timeout
          });
        } catch (error) {
          if (error.message.includes("NODE_TLS_REJECT_UNAUTHORIZED")) {
          }
        }
        if (currentGqlHash) {
          updateGraphqlrcHash(projectRoot, currentGqlHash);
        }
        if (verbose) {
          console.log("[gql-redirect] Successfully executed pnpm gqlgen and updated hash");
        }
        return true;
      } catch (error) {
        console.error(`[gql-redirect] Failed to execute pnpm gqlgen:`, error.message);
        return false;
      }
    }
    function resolvePathAlias(modulePath, pathMappings, resolveDir) {
      const possiblePaths = [];
      for (const mapping of pathMappings) {
        if (modulePath.startsWith(mapping.alias)) {
          const remainingPath = modulePath.slice(mapping.alias.length);
          const cleanRemainingPath = remainingPath.startsWith("/") ? remainingPath.slice(1) : remainingPath;
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
    function gqlRedirectPlugin(options = {}) {
      const { verbose = false, tsconfigPath = "./tsconfig.json", autoGqlGen = true } = options;
      return {
        name: "gql-redirect",
        setup(build) {
          const angularJsonDir = findAngularJsonDir(process.cwd());
          const pathMappings = loadTsconfigPaths(tsconfigPath);
          if (verbose && pathMappings.length > 0) {
            console.log(`[gql-redirect] Loaded ${pathMappings.length} path mappings from ${tsconfigPath}`);
          }
          if (autoGqlGen) {
            runGqlGen(angularJsonDir, verbose);
          }
          build.onResolve({ filter: /\.gql$/ }, (args) => {
            try {
              const possiblePaths = resolvePathAlias(args.path, pathMappings, args.resolveDir);
              for (const resolvedPath of possiblePaths) {
                const gqlTsPath = resolvedPath + ".ts";
                if (fs.existsSync(gqlTsPath)) {
                  if (verbose) {
                    const displayPath = path.relative(angularJsonDir, gqlTsPath) || ".";
                    console.log(`[gql-redirect] Redirecting "${args.path}" to "${displayPath}"`);
                  }
                  return { path: gqlTsPath };
                }
              }
              if (verbose) {
                console.warn(`[gql-redirect] No .gql.ts found for ${args.path} in any of the resolved paths: ${possiblePaths.join(", ")}`);
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
        }
      };
    }
    module.exports = gqlRedirectPlugin;
    module.exports.default = gqlRedirectPlugin;
    module.exports.gqlRedirectPlugin = gqlRedirectPlugin;
  }
});
export default require_gql_redirect_plugin();
