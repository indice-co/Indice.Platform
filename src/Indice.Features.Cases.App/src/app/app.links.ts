import { map } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { ExternalNavLink, IAppLinks, NavLink } from '@indice/ng-components';

import { Observable, of } from 'rxjs';

export class AppLinks implements IAppLinks {
    constructor(private authService: AuthService) {
        this.main = this.authService.user$.pipe(
            map(user => {
                const headerMenu = [
                    new NavLink('Αρχική', '/dashboard', true),
                    new NavLink('Αιτήσεις', '/cases', true),
                    new NavLink('Ειδοποιήσεις', '/notifications', true)
                ]
                if (this.authService.isAdmin()) {
                    headerMenu.push(new NavLink('Διαχείριση Αιτήσεων', '/case-types', true))
                }
                return headerMenu;
            })
        )
    }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    public main: Observable<NavLink[]>;

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
        new ExternalNavLink('ChaniaBank', 'https://www.chaniabank.gr')
    ]);
}
