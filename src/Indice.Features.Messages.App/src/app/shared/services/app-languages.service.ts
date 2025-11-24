import { Inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { catchError, map, Observable, of, take, tap } from 'rxjs';
import { AuthService } from '@indice/ng-auth';
import { HttpClient } from '@angular/common/http';
import { MESSAGES_API_BASE_URL } from '../../core/services/messages-api.service';

@Injectable({
  providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {

  private _languages: MenuOption[] = [];
  public options: Observable<MenuOption[]> | undefined;
  public selected?: string | undefined;
  public default?: string | undefined;

  constructor(private translate: TranslateService, private http: HttpClient, @Inject(AuthService) protected _authService: AuthService, @Inject(MESSAGES_API_BASE_URL) protected _apiBaseUrl: string) {
    this.initializeLanguages();
  }
  private initializeLanguages(): void {
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
            new MenuOption('EL', 'EL', 'Ελληνικά')
          ]);
        }),
        tap(languages => {
          this._languages = languages;
          this.translate.addLangs(this._languages.map(l => l.value.toLowerCase()));
          this.default = this._languages[0]?.value.toLowerCase();
          this.options = of(this._languages);
        })
      )
      .subscribe(() =>
        this.setLocale());
  }
  private setLocale(): void {
    this._authService.loadUser().pipe(take(1)).subscribe((result) => {
      if (result !== null) {
        const userLocale = this._authService.getCurrentUser().profile.locale;
        if (userLocale && this._languages.map(x => x.text).includes(userLocale.toUpperCase())) {
          console.log("Using user locale");
          this.setSelected(userLocale)
        }
      }
      else {
        console.log("No locale found");
        this.setSelected(this.default!);
      }
    });
  }

  public setSelected(lang: string): void {
    this.selected = lang.toUpperCase();
    this.translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: any): Observable<string> {
    return this.translate.get(key || '', parameters);
  }
}

// The 
export interface UiLocale {
  lang: string;
  nativeName: string;
  englishName: string;
}
