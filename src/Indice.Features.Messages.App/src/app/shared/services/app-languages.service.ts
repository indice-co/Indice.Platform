import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {
  private _languages = [
    new MenuOption('EN', 'EN', 'English'),
    new MenuOption('EL', 'EL', 'Ελληνικά')
  ];

  constructor(private translate: TranslateService) {

    this.translate.addLangs(this._languages.map(l => l.value.toLowerCase()));
    this.default = this._languages[0].value.toLowerCase();
    this.options = of(this._languages);
    this.selected = this.default = this._languages[0].value;
    this.translate.use(this.default!);
  }

  public options: Observable<MenuOption[]> | undefined;
  public selected?: string | undefined;
  public default?: string | undefined;

  public setSelected(lang: string): void {
    this.selected = lang;
    this.translate.use(lang.toLowerCase());
  }

  public translateKey(key?: string, parameters?: any): Observable<string> {
    return this.translate.stream(key || '', parameters);
  }

  public translateToaster(title?: string, body?: string, parameters?: any): Observable<string>[] {
    const translatedTitle = this.translateKey(title, parameters);
    const translatedBody = this.translateKey(body, parameters);
    return [translatedTitle, translatedBody];
  }
}
