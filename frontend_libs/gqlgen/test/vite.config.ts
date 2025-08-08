import { defineConfig, PluginOption } from 'vite';
import react from '@vitejs/plugin-react';
import gqlgen from '@geexbox/gqlgen';

// Vite configuration with custom GraphQL typed-document plugin
export default defineConfig({
  plugins: [
    react(),
    gqlgen({
      // optional overrides for testing
      // schemaLocal: ['src/gql/schema.graphql'],
      // schemaRemote: ['https://your-graphql-endpoint.com/graphql'],
      // sharedTypesDir: 'src/graphql',
      // scalars: { URL: 'string' },
    }),
  ],
  build: {
    target: 'esnext',
  },
});
