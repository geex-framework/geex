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
declare function gqlRedirectPlugin(options?: GqlRedirectPluginOptions): Plugin;
declare namespace gqlRedirectPlugin {
    export { gqlRedirectPlugin as default, gqlRedirectPlugin, type Plugin, type GqlRedirectPluginOptions, type PathMapping };
}
type Plugin = any;
type GqlRedirectPluginOptions = {
    /**
     * Whether to enable verbose logging
     */
    verbose?: boolean | undefined;
    /**
     * Path to tsconfig.json file for path alias resolution
     */
    tsconfigPath?: string | undefined;
    /**
     * Whether to automatically run pnpm gqlgen when .gql.ts files are missing
     */
    autoGqlGen?: boolean | undefined;
};
type PathMapping = {
    alias: string;
    paths: string[];
};

export { gqlRedirectPlugin as default };
