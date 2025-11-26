import { Inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { Observable, of, take } from 'rxjs';
import { AuthService } from '@indice/ng-auth';

@Injectable({
  providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {
  private _languages = [
    new MenuOption('EN', 'EN', 'English'),
    new MenuOption('EL', 'EL', 'Ελληνικά')
  ];

  constructor(private translate: TranslateService, @Inject(AuthService) protected _authService: AuthService) {

    this.translate.addLangs(this._languages.map(l => l.value.toLowerCase()));
    this.default = this._languages[0].value.toLowerCase();
    this.options = of(this._languages);
    //this.selected = this.default = this._languages[0].value;
    //this.translate.use(this.default!);
    console.log("This was constructed")

    _authService.isLoggedIn().pipe(take(1)).subscribe((result) =>
    {
      if (result == true) {
        const userLocale = _authService.getCurrentUser().profile.locale;
        if (userLocale && this._languages.map(x => x.text).includes(userLocale.toUpperCase())) {
          console.log("Using selected locale");
          this.setSelected(userLocale)
        }
      }
      else {
        console.log("No locale found");
        this.setSelected(this.default!);
      }
    });


  }

  public options: Observable<MenuOption[]> | undefined;
  public selected?: string | undefined;
  public default?: string | undefined;

  public setSelected(lang: string): void {
    this.selected = lang.toUpperCase();
    this.translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: any): Observable<string> {
    return this.translate.get(key || '', parameters);
  }
}
