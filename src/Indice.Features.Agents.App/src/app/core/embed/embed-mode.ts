import { InjectionToken } from '@angular/core';

import { settings } from '../models/settings';

/**
 * Whether the app runs as the chat widget's `/embed` frame. In embed mode the app is guests only: it never sends a
 * signed-in user's token (a same-origin full UI tab shares its OIDC store with the frame) and keeps its own storage keys.
 * A token rather than a direct `settings` read, so specs can switch it.
 */
export const EMBED_MODE = new InjectionToken<boolean>('EMBED_MODE', {
  providedIn: 'root',
  factory: () => settings.embed,
});

/** Parent origins allowed to frame `/embed` and talk to it over `postMessage` (the page's own origin is always allowed). */
export const WIDGET_ORIGINS = new InjectionToken<readonly string[]>('WIDGET_ORIGINS', {
  providedIn: 'root',
  factory: () => settings.widget_origins,
});
