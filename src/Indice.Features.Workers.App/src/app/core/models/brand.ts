import { signal } from '@angular/core';

import { settings } from './settings';

const root = document.documentElement;
const readTheme = () => root.dataset['theme'] || 'indice';
const theme = signal(settings.theme);

/** The active daisyUI theme — `<html data-theme>`, set by the host (`WorkerDashboardUIOptions.Theme`) and followed when it changes at runtime. */
export const brandTheme = theme.asReadonly();

/** Display name of the dashboard, shown wherever the UI names it. */
export const appName = 'Workers';

// The brand mark is an inline SVG coloured with `text-primary`, so a theme switch needs no asset swap — only the signal.
new MutationObserver(() => theme.set(readTheme())).observe(root, {
  attributes: true,
  attributeFilter: ['data-theme'],
});
