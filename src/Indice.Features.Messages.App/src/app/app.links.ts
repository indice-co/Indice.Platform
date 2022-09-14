import { TenantService } from '@indice/ng-auth';
import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';

export class AppLinks implements IAppLinks {
    private _mainNavLinks: NavLink[] = [
        new NavLink('Αρχική', 'dashboard', false, false, Icons.Dashboard),
        new NavLink('Καμπάνιες', '/campaigns', false, false, Icons.Messages),
        new NavLink('Τύποι Μηνυμάτων', '/message-types', false, false, Icons.Details),
        new NavLink('Λίστες Διανομής', '/distribution-lists', false, false, Icons.TilesView),
        new NavLink('Πρότυπα', '/templates', false, false, Icons.SendEmail)
    ];

    constructor(tenantService: TenantService) {
        if (settings.multitenancy) {
            const url = window.location.pathname.replace(/^(.*?)\//, '');
            const urlSegments = url.split('/');
            const tenantAlias = urlSegments[settings.isTemplate ? 1 : 0];
            tenantService.storeTenant(tenantAlias);
            this._mainNavLinks.forEach((navLink: NavLink) => navLink.path = `${tenantAlias}/${navLink.path}`);
        }
    }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);
    public main: Observable<NavLink[]> = of(this._mainNavLinks);

    public profile: Observable<NavLink[]> = of([
        new NavLink('Αποσύνδεση', '/logout', false)
    ]);

    public legal: Observable<NavLink[]> = of([
        new ExternalNavLink('Ιδιωτικό Απόρρητο', '/privacy'),
        new ExternalNavLink('Όροι χρήσης', '/terms'),
        new ExternalNavLink('Επικοινωνία', '/contact')
    ]);

    public brand: Observable<NavLink[]> = of([
        new ExternalNavLink('Indice', 'https://www.indice.gr')
    ]);
}
