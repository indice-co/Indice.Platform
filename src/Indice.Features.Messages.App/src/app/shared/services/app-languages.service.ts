import { Injectable } from '@angular/core';

import { IAppLanguagesService, MenuOption } from '@indice/ng-components';
import { Observable, of } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AppLanguagesService implements IAppLanguagesService {
    private _languages = [
        new MenuOption('EL', 'EL', 'Ελληνικά'), 
        //new MenuOption('EN', 'EN', 'English')
    ];

    constructor() {
        this.options = of(this._languages);
        this.selected = this.default = this._languages[0].value;
    }

    public options: Observable<MenuOption[]> | undefined;
    public selected?: string | undefined;
    public default?: string | undefined;
    
    public setSelected(language: string): void {
        this.selected = language;
    }
}