# Indice.Hosting.App

The workers dashboard SPA for `Indice.Hosting` (queues, scheduled tasks, servers & locks). Angular 20
(standalone, zoneless, signals), Tailwind 4 + daisyUI 5, OIDC sign-in through `@indice/ng-auth`.

It is embedded in, and served by, the `Indice.Hosting.UI` package (`app.UseWorkerDashboardUI()`).

## Develop

```bash
npm install
npm start
```

Open `http://localhost:4200/`. `ng serve` uses `src/environments/environment.ts`:

- `api_url`: a host that calls `AddWorkerDashboard()` / `MapWorkerDashboard()` and allows CORS for `http://localhost:4200`.
- `auth_settings.authority`: your identity server. The `workers-ui` client must allow the
  `http://localhost:4200/auth-callback`, `/auth-renew` and `/logged-out` redirect URIs and the `workers` scope.

## Build for embedding

```bash
npm run build:template
```

Output goes to `dist/workers-app/browser/`. The template build reads its real configuration (authority, client id,
host, API base, scopes) from the `<app-root>` attributes that `Indice.Hosting.UI` renders into `index.html` at runtime
(see `src/app/core/models/settings.ts` and `src/environments/environment.template.ts`).

A Debug build of `Indice.Hosting.UI` runs this script automatically through `Indice.Hosting.App.esproj`.

## Test

```bash
npx ng test --watch=false --browsers=ChromeHeadless
```
