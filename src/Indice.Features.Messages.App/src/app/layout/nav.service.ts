import { Injectable } from '@angular/core';
import {
  LucideActivity, LucideBookUser, LucideIcon, LucideImages, LucideLayoutDashboard, LucideLayoutTemplate, LucideMegaphone,
  LucideSettings, LucideTags, LucideUsers
} from '@lucide/angular';
import { settings } from '../core/models/settings';

export interface NavItem {
  /** Translation key. */
  label: string;
  link: string;
  icon: LucideIcon;
}

export interface NavSection {
  /** Translation key for the section heading; omitted for the first section. */
  label?: string;
  items: NavItem[];
}

/** Sidebar navigation. The media library entry depends on the `enableMediaLibrary` setting. */
@Injectable({ providedIn: 'root' })
export class NavService {
  public readonly sections: NavSection[] = [
    {
      items: [
        { label: 'Nav.Dashboard', link: '/dashboard', icon: LucideLayoutDashboard },
        { label: 'Nav.Campaigns', link: '/campaigns', icon: LucideMegaphone },
        { label: 'Nav.Templates', link: '/templates', icon: LucideLayoutTemplate }
      ]
    },
    {
      label: 'Nav.Audience',
      items: [
        { label: 'Nav.Contacts', link: '/contacts', icon: LucideUsers },
        { label: 'Nav.DistributionLists', link: '/distribution-lists', icon: LucideBookUser }
      ]
    },
    {
      label: 'Nav.Configuration',
      items: [
        ...(settings.enableMediaLibrary ? [{ label: 'Nav.Media', link: '/media', icon: LucideImages }] : []),
        { label: 'Nav.MessageTypes', link: '/message-types', icon: LucideTags },
        { label: 'Nav.Events', link: '/message-events', icon: LucideActivity },
        { label: 'Nav.Settings', link: '/settings', icon: LucideSettings }
      ]
    }
  ];
}
