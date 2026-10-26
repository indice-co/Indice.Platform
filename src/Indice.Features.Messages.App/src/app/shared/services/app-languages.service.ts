import { Injectable, inject, signal } from '@angular/core';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { LangChangeEvent, TranslateService } from '@ngx-translate/core';
import { AuthService } from '@indice/ng-auth';
import { User } from 'oidc-client-ts';
import { Observable, catchError, filter, map, of, startWith, take } from 'rxjs';
import { MESSAGES_API_BASE_URL } from '../../core/services/messages-api.service';

export interface AppLanguage {
  /** Upper case language code, e.g. `EN`. */
  code: string;
  /** Native name, e.g. `Ελληνικά`. */
  name: string;
}

const FALLBACK_LANGUAGES: AppLanguage[] = [
  { code: 'EN', name: 'English' },
  { code: 'EL', name: 'Ελληνικά' }
];

@Injectable({ providedIn: 'root' })
export class AppLanguagesService {
  private readonly _translate = inject(TranslateService);
  private readonly _auth = inject(AuthService);
  private readonly _apiBaseUrl = inject(MESSAGES_API_BASE_URL);
  // A bare HttpClient over HttpBackend so the languages call bypasses the auth / accept-language
  // interceptors (which themselves depend on this service).
  private readonly _http = new HttpClient(inject(HttpBackend));

  public readonly languages = signal<AppLanguage[]>(FALLBACK_LANGUAGES);
  public readonly selected = signal('EN');

  constructor() {
    this.setSelected('en');
    this.loadAvailableLanguages();
    this._auth.user$
      .pipe(filter((user): user is User => !!user?.profile?.locale), take(1))
      .subscribe(user => {
        const locale = user.profile.locale!.toUpperCase();
        // an unsupported profile locale must not override the default
        if (this.languages().some(l => l.code === locale)) {
          this.setSelected(locale);
        }
      });
  }

  public setSelected(lang: string): void {
    this.selected.set(lang.toUpperCase());
    document.documentElement.lang = lang.toLowerCase();
    this._translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: Record<string, unknown>): Observable<string> {
    return this._translate.stream(key || '', parameters);
  }

  /** Emits once immediately, then on every language change. */
  public onLanguageChange(): Observable<LangChangeEvent | null> {
    return this._translate.onLangChange.pipe(startWith(null));
  }

  private loadAvailableLanguages(): void {
    this._http.get<UiLocale[]>(`${this._apiBaseUrl}/languages`)
      .pipe(
        map(languages => languages.map(l => ({ code: l.lang.toUpperCase(), name: l.nativeName }))),
        catchError(error => {
          console.error('Failed to load languages, using fallback', error);
          return of(FALLBACK_LANGUAGES);
        })
      )
      .subscribe(languages => {
        if (languages.length > 0) {
          this.languages.set(languages);
          this._translate.addLangs(languages.map(l => l.code.toLowerCase()));
        }
      });
  }
}

/** The user interface for a locale */
export interface UiLocale {
  lang: string;
  nativeName: string;
  englishName: string;
}
