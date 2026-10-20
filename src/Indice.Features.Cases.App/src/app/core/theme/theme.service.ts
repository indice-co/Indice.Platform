import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';

export type ThemePreference = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'cases-ui.theme';
const THEMES: Record<Exclude<ThemePreference, 'system'>, string> = { light: 'cases', dark: 'cases-dark' };

/**
 * Light / dark theme switch. `system` (the default) leaves `data-theme` unset so DaisyUI follows the OS preference;
 * an explicit choice is stamped on `<html data-theme>` and remembered in this browser only.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  public readonly preference = signal<ThemePreference>(this.read());

  constructor() {
    effect(() => {
      const preference = this.preference();
      const root = this.document.documentElement;
      if (preference === 'system') {
        root.removeAttribute('data-theme');
      } else {
        root.setAttribute('data-theme', THEMES[preference]);
      }
      this.write(preference);
    });
  }

  public set(preference: ThemePreference): void {
    this.preference.set(preference);
  }

  private read(): ThemePreference {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return value === 'light' || value === 'dark' ? value : 'system';
    } catch {
      return 'system';
    }
  }

  private write(preference: ThemePreference): void {
    try {
      if (preference === 'system') {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, preference);
      }
    } catch {
      // Storage can be unavailable (private mode, blocked site data); the choice then lasts for the session only.
    }
  }
}
