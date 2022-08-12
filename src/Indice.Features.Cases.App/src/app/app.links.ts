import { map } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';

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
        new NavLink('Αποσύνδεση', '/logout', false)
    ]);

    public legal: Observable<NavLink[]> = of([]);
    public brand: Observable<NavLink[]> = of([]);
}
