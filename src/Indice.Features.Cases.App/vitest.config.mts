import { defineConfig } from 'vitest/config';

// Used by `ng test` (see angular.json). @jsonforms/angular imports lodash sub-paths without extensions, which Node's ESM
// resolver rejects; inlining it lets Vite resolve them.
export default defineConfig({
  test: {
    server: {
      deps: {
        inline: [/@jsonforms\/angular/]
      }
    }
  }
});
