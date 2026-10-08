import { computed, signal } from '@angular/core';

import { settings } from './settings';

const root = document.documentElement;
const readTheme = () => root.dataset['theme'] || 'dex';
const theme = signal(settings.theme);

/** The active daisyUI theme — `<html data-theme>`, set by the host (`AgentsUIOptions.Theme`) and followed when it changes at runtime. */
export const brandTheme = theme.asReadonly();

/** Display name of the assistant — `AgentsUIOptions.AssistantName` on the host, `Dex` by default. */
export const assistantName = settings.assistant_name;

/** Brand mark of the active theme. Every theme ships a matching `{theme}-logo.png` (app `public/` or the host's `wwwroot/`). */
export const brandLogo = computed(() => `${theme()}-logo.png`);

const syncFavicon = () => document.querySelector('link[rel="icon"]')?.setAttribute('href', brandLogo());

// The startup theme may differ from the one the host rendered the favicon for (session storage / `?uitheme=`).
syncFavicon();

// A theme switch is a brand switch: follow the attribute and keep the favicon on the same mark.
new MutationObserver(() => {
  theme.set(readTheme());
  syncFavicon();
}).observe(root, { attributes: true, attributeFilter: ['data-theme'] });
