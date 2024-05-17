import { map, tap } from 'rxjs/operators';
import { AuthService } from '@indice/ng-auth';
import { IAppLinks, NavLink } from '@indice/ng-components';
import { Observable, of, ReplaySubject } from 'rxjs';
import { Params } from '@angular/router';
import { CaseTypeService } from './core/services/case-type.service';

export class AppLinks implements IAppLinks {

  private _main: ReplaySubject<NavLink[]> = new ReplaySubject<NavLink[]>(1);

  // constructor(private authService: AuthService, private _api: CasesApiService) {
  //   zip(this.authService.user$.pipe(
  //     map(user => {
  //       const headerMenu = [
  //         new NavLink('Αρχική', '/dashboard', true),
  //         new NavLink('Υποθέσεις', '/cases', true)
  //       ];

  //       if (this.authService.isAdmin()) {
  //         headerMenu.push(new NavLink('Διαχείριση Υποθέσεων', '/case-types', true));
  //       } else {
  //         headerMenu.push(new NavLink('Ειδοποιήσεις', '/notifications', true));
  //       }

  //       return headerMenu;
  //     }),
  //     tap(navLinks => this._main.next(navLinks))
  //   ), this._api.getMenuItems().pipe(
  //     map(data => {
  //       return data.items?.map(item => new NavLink(`${item.title}`, `/menu-items/${item.id}`, true));
  //     })
  //   )
  //   ).pipe(map((navLinks) => {
  //     if (navLinks[1]) {
  //       return navLinks[0].concat(navLinks[1]);
  //     }
  //     return navLinks[0];
  //   })).subscribe();
  // }

  constructor(
    private authService: AuthService,
  private _caseTypeService: CaseTypeService) {
    this.authService.user$.pipe(
      map(user => {
        const headerMenu = [
          new NavLink('Αρχική', '/dashboard', true),
          new NavLink('Υποθέσεις', '/cases', true)
        ];

        if (this.authService.isAdmin()) {
          headerMenu.push(new NavLink('Διαχείριση Υποθέσεων', '/case-types', true));
          this.appendMenuItems(headerMenu);
        } else {
          headerMenu.push(new NavLink('Ειδοποιήσεις', '/notifications', true));
        }

        return headerMenu;
      }),
      tap(navLinks => this._main.next(navLinks))
    ).subscribe();
  }

  appendMenuItems(headerMenu: NavLink[]) {
    this._caseTypeService.getMenuItems().subscribe(data => {
      if (data.items) {
        for (const item of data.items) {
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
            headerMenu.push(new NavLink(item.title, `/cases/${item.code}`, true, undefined, undefined, undefined, queryParams));
          }
        }
      }
    });
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

