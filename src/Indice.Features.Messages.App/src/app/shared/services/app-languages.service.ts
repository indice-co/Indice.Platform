import { Injectable, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {
    public options: Observable<MenuOption[]> | undefined;
    public selected?: string | undefined;
    public default?: string | undefined;

    private _languages = [
        new MenuOption('EL', 'el', 'Ελληνικά'), 
        new MenuOption('EN', 'en', 'English')
    ];

    constructor(private translate: TranslateService, private router: Router) {
        this.options = of(this._languages);
        const selectedCulture = sessionStorage.getItem('culture') || 'el';
        this.default = selectedCulture;
        this.selected = this.default;
    }
    public setSelected(language: string): void {
        this.selected = language;
        this.translate.use(language);
        this.translate.setDefaultLang(language);
        sessionStorage.setItem('culture', language);

        this.router.navigate(['/']).then(() => window.location.reload());
    }
}