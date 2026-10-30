import { DOCUMENT, Injectable, computed, effect, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'risk-ui.theme';
const THEMES = { light: 'risk', dark: 'risk-dark' } as const;

/** Light / dark / system theme switching. The choice is a per-browser convenience kept in localStorage. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _document = inject(DOCUMENT);
  private readonly _media = this._document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
  private readonly _systemDark = signal(this._media?.matches ?? false);

  public readonly mode = signal<ThemeMode>(this.readStoredMode());
  public readonly isDark = computed(() => this.mode() === 'dark' || (this.mode() === 'system' && this._systemDark()));

  constructor() {
    this._media?.addEventListener('change', event => this._systemDark.set(event.matches));
    effect(() => {
      this._document.documentElement.setAttribute('data-theme', this.isDark() ? THEMES.dark : THEMES.light);
    });
  }

  public setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // storage unavailable (private mode, blocked site data): the choice simply won't persist
    }
  }

  private readStoredMode(): ThemeMode {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'light' || stored === 'dark' || stored === 'system' ? stored : 'system';
    } catch {
      return 'system';
    }
  }
}
