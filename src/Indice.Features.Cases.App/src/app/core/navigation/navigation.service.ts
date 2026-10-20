import { Injectable, signal } from '@angular/core';
import { Params } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { filter, switchMap, take } from 'rxjs/operators';
import { AppIcon } from 'src/app/ui/icons';
import { CaseTypeService } from '../services/case-type.service';

export interface NavItem {
  /** Translation key, or plain text when `translate` is false. */
  label: string;
  translate: boolean;
  link: string;
  icon?: AppIcon;
  queryParams?: Params;
  children?: NavItem[];
}

export interface NavGroup {
  /** Translation key of the group heading; the first group has none. */
  label?: string;
  items: NavItem[];
}

/**
 * The sidebar navigation. Case types flagged as menu items appear under "Cases" and open the case list of that type;
 * administration links are shown to admins and notification settings to case managers and administrators.
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {
  public readonly groups = signal<NavGroup[]>(this.build([], false, false));

  constructor(authService: AuthService, caseTypeService: CaseTypeService) {
    authService.user$.pipe(
      filter(user => !!user),
      take(1),
      switchMap(() => caseTypeService.getCaseTypeMenuItems())
    ).subscribe(menuItems => {
      const caseTypes = menuItems
        .filter(item => !!item.title)
        .map(item => ({ title: item.title!, code: item.code! }));
      const isAdmin = authService.isAdmin();
      const canSeeNotifications = isAdmin || authService.hasRole('CasesManager') || authService.hasRole('CasesAdministrator');
      this.groups.set(this.build(caseTypes, isAdmin, canSeeNotifications));
    });
  }

  private build(caseTypes: { title: string; code: string }[], isAdmin: boolean, canSeeNotifications: boolean): NavGroup[] {
    const groups: NavGroup[] = [{
      items: [
        { label: 'nav.dashboard', translate: true, link: '/dashboard', icon: 'lucideLayoutDashboard' },
        {
          label: 'nav.cases', translate: true, link: '/cases', icon: 'lucideFolders',
          // Case-type titles come from the server, already in the user's language.
          children: caseTypes.map(caseType => ({
            label: caseType.title,
            translate: false,
            link: `/case/by-type/${caseType.code}`,
            queryParams: {
              view: 'table',
              page: '1',
              pagesize: '20',
              search: '',
              sort: 'createdByWhen',
              dir: 'desc',
              filter: `caseTypeCodes::eq::${caseType.code}`
            }
          }))
        }
      ]
    }];
    if (isAdmin) {
      groups.push({
        label: 'nav.administration',
        items: [
          { label: 'nav.caseTypes', translate: true, link: '/case-types', icon: 'lucideLayers' },
          { label: 'nav.checkpointTypes', translate: true, link: '/checkpoint-types', icon: 'lucideFlag' },
          { label: 'nav.accessRules', translate: true, link: '/access-rules', icon: 'lucideShieldCheck' }
        ]
      });
    }
    if (canSeeNotifications) {
      groups.push({
        label: 'nav.settings',
        items: [
          { label: 'nav.notifications', translate: true, link: '/notifications', icon: 'lucideBell' }
        ]
      });
    }
    return groups;
  }
}
