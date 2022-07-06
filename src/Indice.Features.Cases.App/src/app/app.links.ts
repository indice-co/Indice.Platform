import { ExternalNavLink, IAppLinks, NavLink } from '@indice/ng-components';

import { Observable, of } from 'rxjs';

export class AppLinks implements IAppLinks {
    constructor() { }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    public main: Observable<NavLink[]> = of([
        new NavLink('Αρχική', '/dashboard', true),
        new NavLink('Αιτήσεις', '/cases', true),
        new NavLink('Ειδοποιήσεις', '/notifications', true)
    ]);

    public profile: Observable<NavLink[]> = of([
        // new NavLink('Προφίλ', '/profile', true),
        // new NavLink('Ρυθμίσεις', '/settings', false),
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
