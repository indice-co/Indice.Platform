import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';

export class AppLinks implements IAppLinks {
    constructor() { }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    private _mainLInks = [
        new NavLink('general.home', 'dashboard', false, false, Icons.Dashboard),
        new NavLink('general.campaigns', '/campaigns', false, false, Icons.Messages),
        new NavLink('general.message-types', '/message-types', false, false, Icons.Details),
        new NavLink('general.distribution-lists', '/distribution-lists', false, false, Icons.TilesView),
        new NavLink('general.templates', '/templates', false, false, Icons.SendEmail),
        new NavLink('general.files', '/media', false, false, 'ms-Icon ms-Icon--Folder'),
        new NavLink('general.options', '/settings', false, false, 'ms-Icon ms-Icon--Settings')
    ];
    public main: Observable<NavLink[]> = of(settings.enableMediaLibrary ? this._mainLInks : this._mainLInks.filter((l) => l.path !== '/media'));

    public profile: Observable<NavLink[]> = of([
        new NavLink('general.logout', '/logout', false)
    ]);

    public legal: Observable<NavLink[]> = of([
        new ExternalNavLink('general.privacy-policy', '/privacy'),
        new ExternalNavLink('general.terms-conditions', '/terms'),
        new ExternalNavLink('general.contact', '/contact')
    ]);

    public brand: Observable<NavLink[]> = of([
        new ExternalNavLink('Indice', 'https://www.indice.gr')
    ]);
}
