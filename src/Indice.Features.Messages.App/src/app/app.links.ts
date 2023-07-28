import { ExternalNavLink, IAppLinks, Icons, NavLink } from '@indice/ng-components';
import { Observable, of } from 'rxjs';

export class AppLinks implements IAppLinks {
    constructor() { }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    public main: Observable<NavLink[]> = of([
        new NavLink('Αρχική', 'dashboard', false, false, Icons.Dashboard),
        new NavLink('Καμπάνιες', '/campaigns', false, false, Icons.Messages),
        new NavLink('Τύποι Μηνυμάτων', '/message-types', false, false, Icons.Details),
        new NavLink('Λίστες Διανομής', '/distribution-lists', false, false, Icons.TilesView),
        new NavLink('Πρότυπα', '/templates', false, false, Icons.SendEmail),
        new NavLink('Ρυθμίσεις', '/settings', false, false, 'ms-Icon ms-Icon--Settings')
    ]);

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
