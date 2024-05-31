import { map, switchMap } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';
import { Observable, of, ReplaySubject } from 'rxjs';
import { Params } from '@angular/router';
import { CaseTypeService } from './core/services/case-type.service';
import { Icons } from './shared/icons';

export class AppLinks implements IAppLinks {

  private _main: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);

  constructor(
    private authService: AuthService,
    private _caseTypeService: CaseTypeService
  ) {
    this.authService.user$.pipe(
      switchMap(user => user ? this.getAuthenticatedUserLinks() : this.getUnauthenticatedUserLinks())
    ).subscribe(navLinks => this._main.next(navLinks));
  }

  private getAuthenticatedUserLinks(): Observable<NavLink[]> {
    return this._caseTypeService.getCaseTypeMenuItems().pipe(
      map(caseTypeMenuItems => {
        const headerMenu = this.getBaseHeaderMenu();
        for (const item of caseTypeMenuItems) {
          if (item.title && item.code) {
            const queryParams: Params = {
              view: 'table',
              page: '1',
              pagesize: '10',
              search: '',
              sort: 'createdByWhen',
              dir: 'desc',
              filter: `caseTypeCodes::eq::${item.code}`
            }
            headerMenu.push(new NavLink(item.title, `/cases/${item.code}`, true, undefined, Icons.MenuItem, undefined, queryParams));
          }
        }
        headerMenu.push(new NavLink('Διαχείριση Υποθέσεων', '/case-types', true, undefined, Icons.CaseTypes));
        return headerMenu;
      })
    );
  }

  private getUnauthenticatedUserLinks(): Observable<NavLink[]> {
    const headerMenu = this.getBaseHeaderMenu();
    headerMenu.push(new NavLink('Ειδοποιήσεις', '/notifications', true, undefined, Icons.Notifications));
    return of(headerMenu);
  }

  private getBaseHeaderMenu(): NavLink[] {
    return [
      new NavLink('Αρχική', '/dashboard', true, undefined, Icons.Dashboard),
      new NavLink('Υποθέσεις', '/cases', true, undefined, Icons.Cases),
    ];
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
