import { DOCUMENT, Injectable, inject, signal } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { catchError, firstValueFrom, map, of } from 'rxjs';
import { settings } from '../models/settings';

export interface AppLanguage {
  /** Two letter language code, e.g. `el`. */
  code: string;
  /** Native name, e.g. `Ελληνικά`. */
  name: string;
}

/** The language the UI starts with and falls back to for missing keys. */
export const DEFAULT_LANGUAGE = 'en';
const STORAGE_KEY = 'iam-ui.lang';
const FALLBACK_LANGUAGES: AppLanguage[] = [
  { code: 'en', name: 'English' },
  { code: 'el', name: 'Ελληνικά' }
];

/** The language chosen by the user on this browser or the default one. Resolved once at bootstrap. */
export const initialLanguage = (): string => {
  const stored = localStorage.getItem(STORAGE_KEY)?.toLowerCase();
  return stored && FALLBACK_LANGUAGES.some(language => language.code === stored) ? stored : settings.lang?.toLowerCase() || DEFAULT_LANGUAGE;
};

/** Angular locale (dates, numbers) matching a UI language. */
export const localeOf = (language: string): string => language === 'el' ? 'el' : 'en-GB';

/** Loads the translations served by the identity server (resx → json) and switches the UI language. */
@Injectable({ providedIn: 'root' })
export class AppLanguagesService {
  private readonly _translate = inject(TranslateService);
  private readonly _document = inject(DOCUMENT);
  // bypasses the interceptors: the call is anonymous and runs before the user is known
  private readonly _http = new HttpClient(inject(HttpBackend));

  public readonly languages = signal<AppLanguage[]>(FALLBACK_LANGUAGES);
  public readonly current = signal(initialLanguage());

  /** Runs as an app initializer so that the first render already has its translations. */
  public init(): Promise<unknown> {
    this._document.documentElement.lang = this.current();
    this._http.get<UiLocale[]>(`${settings.api_url}/api/languages`)
      .pipe(
        map(locales => locales.map(locale => ({ code: locale.lang.toLowerCase(), name: locale.nativeName }))),
        catchError(() => of(FALLBACK_LANGUAGES))
      )
      .subscribe(languages => {
        if (languages.length > 0) {
          this.languages.set(languages);
        }
      });
    return firstValueFrom(this._translate.use(this.current()).pipe(catchError(() => of(null))));
  }

  /** Persists the choice and reloads, so that the locale of dates and numbers follows the language too. */
  public use(language: string): void {
    if (language === this.current()) {
      return;
    }
    localStorage.setItem(STORAGE_KEY, language);
    this._document.location.reload();
  }
}

/** A culture supported by the server, as returned by the <code>/languages</code> endpoint. */
interface UiLocale {
  lang: string;
  nativeName: string;
  englishName: string;
}
