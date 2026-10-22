# CasesApp

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 13.0.1.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The app will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Build with selected configuration

```ng build --configuration=on-premises```
```ng build --configuration=on-premises-uat```
```ng build --configuration=on-premises-dev```

## UI and theming

The UI is built with [Tailwind CSS 4](https://tailwindcss.com) + [daisyUI 5](https://daisyui.com) and [Lucide](https://lucide.dev) icons (`@lucide/angular`, one standalone component per icon: `<svg lucideFolderOpen class="size-4"></svg>`).

- Themes `cases` (light) and `cases-dark` are defined in `src/styles.css` with daisyUI theme variables (`--color-primary`, `--color-base-100`, …). Re-brand by overriding those.
- A few app-level variables cover what daisyUI has no token for: `--app-sidebar-*` (sidebar chrome), `--app-highlight` (the highlighter accent used for active / attention states) and `--app-chart-1..6` (chart series).
- The light / dark / system choice is stored per browser (`ThemeService`).
- Reusable building blocks live in `src/app/ui`: `ListState` (URL-synced paging / sorting / filtering), `FilterBar`, `Pagination`, `SearchInput`, `EmptyState`, `PageHeader`, `DialogService` (incl. `confirm()`), `ToastService` and the `rightpane` side pane.
- UI strings are served by the Cases server (`Indice.Features.Cases.Server/Resources/Cases.Ui.TranslationApi*.resx`).

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.io/cli) page.
