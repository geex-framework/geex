import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import gqlgen from '@geexbox/gqlgen';

// Vite configuration with custom GraphQL typed-document plugin
export default defineConfig({
  plugins: [
    react(),
    gqlgen({
      // optional overrides for testing
      // sharedTypesDir: 'src/graphql',
      // scalars: { URL: 'string' }, { Long: { input: BigInt; output: BigInt; }}
      localSchemaMap:{
        'https://api.dev.geexcode.com/graphql': 'schemas/api.dev.geexcode.com.schema.graphql',
        // 'https://api1.dev.geexcode.com/graphql': 'schemas/api1.dev.geexcode.com.schema.graphql',
      }
    }),
  ],
  build: {
    target: 'esnext',
  },
});
