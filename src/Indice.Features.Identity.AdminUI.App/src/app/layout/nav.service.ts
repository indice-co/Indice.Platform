import { Injectable, computed, inject } from '@angular/core';
import {
  LucideActivity, LucideAppWindow, LucideBoxes, LucideIdCard, LucideIcon, LucideKeyRound, LucideLayoutDashboard, LucideLogIn,
  LucideShieldCheck, LucideSlidersHorizontal, LucideTags, LucideUsers
} from '@lucide/angular';
import { Features } from '../core/models/features';
import { AuthService } from '../core/services/auth.service';
import { Permission, hasPermission } from '../core/services/auth.guards';
import { UiFeaturesService, isFeatureEnabled } from '../core/services/ui-features.service';

export interface NavItem {
  /** Translation key. */
  label: string;
  link: string;
  icon: LucideIcon;
  /** Any of these permissions shows the item. */
  permissions: Permission[];
  feature?: Features;
}

export interface NavSection {
  /** Section heading translation key; omitted for the first section. */
  label?: string;
  items: NavItem[];
}

const SECTIONS: NavSection[] = [
  {
    items: [
      { label: 'Routes.Dashboard', link: '/dashboard', icon: LucideLayoutDashboard, permissions: ['usersReader', 'clientsReader'] }
    ]
  },
  {
    label: 'Nav.Identity',
    items: [
      { label: 'Routes.Users', link: '/users', icon: LucideUsers, permissions: ['usersReader'] },
      { label: 'Routes.Roles', link: '/roles', icon: LucideShieldCheck, permissions: ['usersReader'] },
      { label: 'Routes.ClaimTypes', link: '/claim-types', icon: LucideTags, permissions: ['usersReader', 'clientsReader'] }
    ]
  },
  {
    label: 'Nav.Applications',
    items: [
      { label: 'Routes.Clients', link: '/clients', icon: LucideAppWindow, permissions: ['clientsReader'] },
      { label: 'Routes.IdentityResources', link: '/resources/identity', icon: LucideIdCard, permissions: ['clientsReader'] },
      { label: 'Routes.ApiResources', link: '/resources/api', icon: LucideBoxes, permissions: ['clientsReader'] },
      { label: 'Routes.ApiScopes', link: '/resources/scopes', icon: LucideKeyRound, permissions: ['clientsReader'] }
    ]
  },
  {
    label: 'Nav.Monitoring',
    items: [
      { label: 'Routes.SignInLogs', link: '/sign-in-logs', icon: LucideLogIn, permissions: ['admin'], feature: Features.SignInLogs },
      { label: 'Routes.ActivityLogs', link: '/activity-logs', icon: LucideActivity, permissions: ['admin'], feature: Features.ActivityLogs }
    ]
  },
  {
    label: 'Nav.System',
    items: [
      { label: 'Routes.AppSettings', link: '/settings', icon: LucideSlidersHorizontal, permissions: ['admin'] }
    ]
  }
];

/** Sidebar navigation filtered by the user's permissions and the server's UI feature switches. */
@Injectable({ providedIn: 'root' })
export class NavService {
  private readonly _auth = inject(AuthService);
  private readonly _features = inject(UiFeaturesService);

  public readonly sections = computed<NavSection[]>(() => {
    this._auth.currentUser();
    const features = this._features.features();
    return SECTIONS
      .map(section => ({
        ...section,
        items: section.items.filter(item =>
          item.permissions.some(permission => hasPermission(this._auth, permission)) &&
          (!item.feature || isFeatureEnabled(features, item.feature)))
      }))
      .filter(section => section.items.length > 0);
  });
}
