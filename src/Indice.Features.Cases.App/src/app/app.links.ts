import { map, switchMap, toArray } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';
import { Observable, ReplaySubject, combineLatest, of } from 'rxjs';
import { Params } from '@angular/router';
import { CaseTypeService } from './core/services/case-type.service';
import { Icons } from './shared/icons';

export class AppLinks implements IAppLinks {

  private _main: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);

  constructor(
    private authService: AuthService,
    private _caseTypeService: CaseTypeService
  ) {
    const isAdmin$ = this.authService.user$.pipe(
      switchMap(user => of(this.authService.isAdmin()))
    );

    const caseTypeMenuItems$ = isAdmin$.pipe(
      switchMap(isAdmin => isAdmin ? this._caseTypeService.getCaseTypeMenuItems() : of([]))
    );

    combineLatest([isAdmin$, caseTypeMenuItems$]).pipe(
      map(([isAdmin, caseTypeMenuItems]) => {
        const headerMenu: NavLink[] = [
          new NavLink('Αρχική', '/dashboard', true, undefined, Icons.Dashboard),
          new NavLink('Υποθέσεις', '/cases', true, undefined, Icons.Cases),
        ];

        if (isAdmin) {
          for (const item of caseTypeMenuItems) {
            if (item.title) {
              const queryParams: Params = {
                view: 'table',
                page: '1',
                pagesize: '10',
                search: '',
                sort: 'createdByWhen',
                dir: 'desc',
                filter: `caseTypeCodes::eq::${item.code}`
              };
              headerMenu.push(new NavLink(item.title, `/cases/${item.code}`, true, undefined, Icons.MenuItem, undefined, queryParams));
            }
          }
          headerMenu.push(new NavLink('Διαχείριση Υποθέσεων', '/case-types', true, undefined, Icons.CaseTypes));
        } else {
          headerMenu.push(new NavLink('Ειδοποιήσεις', '/notifications', true, undefined, Icons.Notifications));
        }

        return headerMenu;
      })
    ).subscribe(navLinks => this._main.next(navLinks));
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
