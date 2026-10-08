import { Inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { catchError, map, Observable, of, Subject, take, tap } from 'rxjs';
import { AuthService } from '@indice/ng-auth';
import { HttpBackend, HttpClient } from '@angular/common/http';
import { MESSAGES_API_BASE_URL } from '../../core/services/messages-api.service';
import { User } from 'oidc-client-ts';

@Injectable({
  providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {

  private _languages: MenuOption[] = [];
  public options: Observable<MenuOption[]>;
  public selected?: string | undefined;
  public default?: string | undefined;
  private _destroy$ = new Subject<void>();
  private http: HttpClient
  constructor(private translate: TranslateService, private httpBackendHandler: HttpBackend, @Inject(AuthService) protected _authService: AuthService, @Inject(MESSAGES_API_BASE_URL) protected _apiBaseUrl: string) {
    this.http = new HttpClient(this.httpBackendHandler);
    this.options = of([new MenuOption('EN', 'EN', 'English'), new MenuOption('EL', 'EL', 'Ελληνικά')]);
    this._languages = [new MenuOption('EN', 'EN', 'English'), new MenuOption('EL', 'EL', 'Ελληνικά')];
    this.default = this._languages[0]?.value.toLowerCase();
    this.setSelected('en');
    this.loadAvailableLanguages();
    this.setUserLocale();
  }
  private setUserLocale(): void {
    this._authService.user$.subscribe((user) =>
      this.setLocale(user));
  }

  loadAvailableLanguages(): Observable<MenuOption[]> {
    this.http.get<UiLocale[]>(this._apiBaseUrl + '/languages')
      .pipe(
        take(1),
        map(languages => languages.map(langInfo =>
          new MenuOption(langInfo.lang.toUpperCase(), langInfo.lang.toUpperCase(), langInfo.nativeName)
        )),
        catchError(error => {
          console.error('Failed to load languages, using fallback', error);
          return of([
            new MenuOption('EN', 'EN', 'English'),
            new MenuOption('EL', 'EL', 'Ελληνικά'),
          ]);
        }),
        tap(languages => {
          if (languages && languages.length > 0) {
            this._languages = languages;
            this.translate.addLangs(this._languages.map(l => l.value.toLowerCase()));
            this.options = of(this._languages);
          }
        })).subscribe();
    return of(this._languages);
  }

  private setLocale(user: User | null): void {
    if (user && user.profile) {
        const userLocale = user.profile.locale;
      if (userLocale && this._languages.map(x => x.value).includes(userLocale.toUpperCase())) {
          this.setSelected(userLocale);
        }
        else {
          if (!this.selected) this.setSelected(this.default!);
        }
      }
    else {
        if (!this.selected) {
          this.setSelected(this.default!);
        }
      }
  }

  public setSelected(lang: string): void {
    this.selected = lang.toUpperCase();
    this.translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: any): Observable<string> {
    return this.translate.stream(key || '', parameters);
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }
}

// The user interface for a locale
export interface UiLocale {
  lang: string;
  nativeName: string;
  englishName: string;
}
