import { filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';
import { Observable, of, ReplaySubject } from 'rxjs';
import { Params } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { CaseTypeService } from './core/services/case-type.service';
import { AppLanguagesService } from './shared/services/app-languages.service';
import { Icons } from './shared/icons';

export class AppLinks implements IAppLinks {

    private _main: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);
    private _profile: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);

    // Cached state so the menu can be rebuilt (e.g. on language change) without re-fetching.
    private _caseTypeMenuItems: { title?: string; code?: string }[] = [];
    private _isAdmin = false;
    private _canSeeNotifications = false;

    constructor(
        private authService: AuthService,
        private _caseTypeService: CaseTypeService,
        private _translate: TranslateService,
        private _lang: AppLanguagesService
    ) {
        // Initial render (static items only) until the dynamic case types resolve.
        this.rebuild();
        this.authService.user$.pipe(
            filter(user => !!user),
            take(1),
            switchMap(() => this._caseTypeService.getCaseTypeMenuItems())
        ).subscribe(caseTypeMenuItems => {
            this._caseTypeMenuItems = caseTypeMenuItems.filter(item => !!item.title);
            this._isAdmin = this.authService.isAdmin();
            this._canSeeNotifications = this.authService.isAdmin()
                || this.authService.hasRole('CasesManager')
                || this.authService.hasRole('CasesAdministrator');
            this.rebuild();
        });
        // Rebuild every label whenever the language changes (emits once immediately, then on each switch).
        this._lang.onLanguageChange().subscribe(() => this.rebuild());
    }

    /** Rebuilds the nav from cached state, translating the static labels for the current language. */
    private rebuild(): void {
        const menu: NavLink[] = [
            new NavLink(this._translate.instant('nav.dashboard'), '/dashboard', true, undefined, Icons.Dashboard),
            new NavLink(this._translate.instant('nav.cases'), '/cases', true, undefined, Icons.Cases),
        ];
        for (const item of this._caseTypeMenuItems) {
            const queryParams: Params = {
                view: 'table',
                page: '1',
                pagesize: '20',
                search: '',
                sort: 'createdByWhen',
                dir: 'desc',
                filter: `caseTypeCodes::eq::${item.code}`
            };
            // Dynamic case-type titles come from the backend (already language-aware) — do not translate.
            menu.push(new NavLink(item.title!, `/case/by-type/${item.code}`, true, undefined, Icons.MenuItem, undefined, queryParams));
        }
        if (this._isAdmin) {
            menu.push(new NavLink(this._translate.instant('nav.caseTypes'), '/case-types', true, undefined, Icons.CaseTypes));
        }
        if (this._canSeeNotifications) {
            menu.push(new NavLink(this._translate.instant('nav.notifications'), '/notifications', true, undefined, Icons.Notifications));
        }
        this._main.next(menu);
        this._profile.next([
            new NavLink(this._translate.instant('nav.logout'), '/logout', false)
        ]);
    }

    public public: Observable<NavLink[]> = of([]);
    public profileActions: Observable<NavLink[]> = of([]);
    public main: Observable<NavLink[]> = this._main.asObservable();
    public profile: Observable<NavLink[]> = this._profile.asObservable();
    public legal: Observable<NavLink[]> = of([]);
    public brand: Observable<NavLink[]> = of([]);
}
