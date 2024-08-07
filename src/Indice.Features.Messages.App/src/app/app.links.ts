import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { settings } from './core/models/settings';

export class AppLinks implements IAppLinks {
    constructor() { }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    private _mainLInks = [
        new NavLink('Αρχική', 'dashboard', false, false, Icons.Dashboard),
        new NavLink('Καμπάνιες', '/campaigns', false, false, Icons.Messages),
        new NavLink('Τύποι Μηνυμάτων', '/message-types', false, false, Icons.Details),
        new NavLink('Λίστες Διανομής', '/distribution-lists', false, false, Icons.TilesView),
        new NavLink('Πρότυπα', '/templates', false, false, Icons.SendEmail),
        new NavLink('Αρχεία', '/media', false, false, 'ms-Icon ms-Icon--Folder'),
        new NavLink('Ρυθμίσεις', '/settings', false, false, 'ms-Icon ms-Icon--Settings')
    ];
    public main: Observable<NavLink[]> = of(settings.enableMediaLibrary ? this._mainLInks : this._mainLInks.filter((l) => l.path !== '/media'));

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
