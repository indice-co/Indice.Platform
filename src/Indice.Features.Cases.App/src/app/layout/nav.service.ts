import { Injectable, computed, inject, signal } from '@angular/core';
import { Params } from '@angular/router';
import { AuthService } from '@indice/ng-auth';
import { filter, switchMap, take } from 'rxjs';
import {
  LucideBell, LucideFolderKanban, LucideFolderOpen, LucideIcon, LucideLayoutDashboard, LucideMilestone, LucideShapes,
  LucideShieldCheck
} from '@lucide/angular';
import { CaseTypeService } from '../core/services/case-type.service';

export interface NavItem {
  /** Translation key, or plain text when `translate` is false. */
  label: string;
  translate: boolean;
  link: string;
  icon: LucideIcon;
  queryParams?: Params;
}

export interface NavSection {
  /** Translation key for the section heading; omitted for the first section. */
  label?: string;
  items: NavItem[];
}

/** Sidebar navigation, built from the signed-in user's roles and the case types flagged as menu items. */
@Injectable({ providedIn: 'root' })
export class NavService {
  private readonly _auth = inject(AuthService);
  private readonly _caseTypes = inject(CaseTypeService);
  private readonly _menuCaseTypes = signal<{ title: string; code: string }[]>([]);
  private readonly _isAdmin = signal(false);
  private readonly _canSeeNotifications = signal(false);

  public readonly sections = computed<NavSection[]>(() => {
    const sections: NavSection[] = [{
      items: [
        { label: 'nav.dashboard', translate: true, link: '/dashboard', icon: LucideLayoutDashboard },
        { label: 'nav.cases', translate: true, link: '/cases', icon: LucideFolderOpen }
      ]
    }];
    const caseTypes = this._menuCaseTypes();
    if (caseTypes.length) {
      sections.push({
        label: 'nav.caseQueues',
        items: caseTypes.map(item => ({
          // case type titles come from the backend, already localized
          label: item.title,
          translate: false,
          link: `/case/by-type/${item.code}`,
          icon: LucideFolderKanban,
          queryParams: { page: 1, pagesize: 20, sort: 'createdByWhen', dir: 'desc', filter: `caseTypeCodes::eq::${item.code}` }
        }))
      });
    }
    const admin: NavItem[] = [];
    if (this._isAdmin()) {
      admin.push(
        { label: 'nav.caseTypes', translate: true, link: '/case-types', icon: LucideShapes },
        { label: 'nav.checkpointTypes', translate: true, link: '/checkpoint-types', icon: LucideMilestone },
        { label: 'nav.accessRules', translate: true, link: '/access-rules', icon: LucideShieldCheck }
      );
    }
    if (this._canSeeNotifications()) {
      admin.push({ label: 'nav.notifications', translate: true, link: '/notifications', icon: LucideBell });
    }
    if (admin.length) {
      sections.push({ label: 'nav.administration', items: admin });
    }
    return sections;
  });

  constructor() {
    this._auth.user$
      .pipe(
        filter(user => !!user),
        take(1),
        switchMap(() => this._caseTypes.getCaseTypeMenuItems())
      )
      .subscribe(items => {
        this._menuCaseTypes.set(items.filter(i => !!i.title).map(i => ({ title: i.title!, code: i.code! })));
        this._isAdmin.set(this._auth.isAdmin());
        this._canSeeNotifications.set(this._auth.isAdmin() || this._auth.hasRole('CasesManager') || this._auth.hasRole('CasesAdministrator'));
      });
  }
}
