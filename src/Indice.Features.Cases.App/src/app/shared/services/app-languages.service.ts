import { HttpBackend, HttpClient } from '@angular/common/http';
import { Inject, Injectable, signal } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { LangChangeEvent, TranslateService } from '@ngx-translate/core';
import { User } from 'oidc-client-ts';
import { catchError, filter, map, Observable, of, startWith, take } from 'rxjs';
import { CASES_API_BASE_URL } from '../../core/services/cases-api.service';

export interface LanguageOption {
  /** Upper-case code, e.g. `EL`. */
  code: string;
  /** The language in its own name, e.g. `Ελληνικά`. */
  nativeName: string;
}

const FALLBACK_LANGUAGES: LanguageOption[] = [
  { code: 'EN', nativeName: 'English' },
  { code: 'EL', nativeName: 'Ελληνικά' }
];

/**
 * The UI languages: the ones the server offers (`/languages`), the selected one, and the user's profile locale applied
 * once after sign-in when the server supports it.
 */
@Injectable({ providedIn: 'root' })
export class AppLanguagesService {
  private readonly http: HttpClient;
  public readonly languages = signal<LanguageOption[]>(FALLBACK_LANGUAGES);
  /** Upper-case code of the selected language. */
  public readonly selected = signal('EN');

  constructor(
    private readonly translate: TranslateService,
    httpBackend: HttpBackend,
    @Inject(AuthService) private readonly authService: AuthService,
    @Inject(CASES_API_BASE_URL) private readonly apiBaseUrl: string
  ) {
    // A bare HttpClient over HttpBackend: the languages call must bypass the auth / accept-language interceptors,
    // which themselves depend on this service.
    this.http = new HttpClient(httpBackend);
    this.setSelected('en');
    this.loadAvailableLanguages();
    this.authService.user$
      .pipe(filter((user): user is User => !!user?.profile?.locale), take(1))
      .subscribe(user => this.applyUserLocale(user));
  }

  public setSelected(lang: string): void {
    this.selected.set(lang.toUpperCase());
    this.translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: object): Observable<string> {
    return this.translate.stream(key || '', parameters);
  }

  /**
   * Emits once immediately, then on every language change. Labels built in TypeScript with `instant()` subscribe to
   * this to re-translate without a reload.
   */
  public onLanguageChange(): Observable<LangChangeEvent | null> {
    return this.translate.onLangChange.pipe(startWith(null));
  }

  private loadAvailableLanguages(): void {
    this.http.get<UiLocale[]>(`${this.apiBaseUrl}/languages`)
      .pipe(
        take(1),
        map(languages => languages.map(l => ({ code: l.lang.toUpperCase(), nativeName: l.nativeName }))),
        catchError(error => {
          console.error('Failed to load languages, using fallback', error);
          return of(FALLBACK_LANGUAGES);
        })
      )
      .subscribe(languages => {
        if (languages.length > 0) {
          this.languages.set(languages);
          this.translate.addLangs(languages.map(l => l.code.toLowerCase()));
        }
      });
  }

  private applyUserLocale(user: User): void {
    const locale = user.profile.locale!.toUpperCase();
    // An unsupported profile locale keeps the default.
    if (this.languages().some(l => l.code === locale)) {
      this.setSelected(locale);
    }
  }
}

/** A UI locale as returned by the server. */
export interface UiLocale {
  lang: string;
  nativeName: string;
  englishName: string;
}
