import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';
import { TranslateService } from '@ngx-translate/core';
import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})

export class AppLinks implements IAppLinks {
    constructor(
        private _translate: TranslateService,
    ) { }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    private _mainLInks = [
        new NavLink(this._translate.instant('general.home'), 'dashboard', false, false, Icons.Dashboard),
        new NavLink(this._translate.instant('general.campaigns'), '/campaigns', false, false, Icons.Messages),
        new NavLink(this._translate.instant('general.message-types'), '/message-types', false, false, Icons.Details),
        new NavLink(this._translate.instant('general.distribution-lists'), '/distribution-lists', false, false, Icons.TilesView),
        new NavLink(this._translate.instant('general.templates'), '/templates', false, false, Icons.SendEmail),
        new NavLink(this._translate.instant('general.files'), '/media', false, false, 'ms-Icon ms-Icon--Folder'),
        new NavLink(this._translate.instant('general.options'), '/settings', false, false, 'ms-Icon ms-Icon--Settings')
    ];
    public main: Observable<NavLink[]> = of(settings.enableMediaLibrary ? this._mainLInks : this._mainLInks.filter((l) => l.path !== '/media'));

    public profile: Observable<NavLink[]> = of([
        new NavLink(this._translate.instant('general.logout'), '/logout', false)
    ]);

    public legal: Observable<NavLink[]> = of([
        new ExternalNavLink(this._translate.instant('general.privacy-policy'), '/privacy'),
        new ExternalNavLink(this._translate.instant('general.terms-conditions'), '/terms'),
        new ExternalNavLink(this._translate.instant('general.contact'), '/contact')
    ]);

    public brand: Observable<NavLink[]> = of([
        new ExternalNavLink('Indice', 'https://www.indice.gr')
    ]);
}
