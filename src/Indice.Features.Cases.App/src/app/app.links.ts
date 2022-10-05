import { map, tap } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';

import { Observable, of, ReplaySubject } from 'rxjs';

export class AppLinks implements IAppLinks {

    private _main: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);

    constructor(private authService: AuthService) {
        this.authService.user$.pipe(
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
            }),
            tap(navLinks => this._main.next(navLinks))
        ).subscribe();
    }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);

    public main: Observable<NavLink[]> = this._main.asObservable();

    public profile: Observable<NavLink[]> = of([
        new NavLink('Αποσύνδεση', '/logout', false)
    ]);

    public legal: Observable<NavLink[]> = of([]);
    public brand: Observable<NavLink[]> = of([]);
}

