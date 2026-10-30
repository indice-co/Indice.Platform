import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideDynamicIcon, LucideIcon } from '@lucide/angular';

export interface RouteTab {
  label: string;
  /** Router link, relative to the hosting route. */
  link: string;
  icon?: LucideIcon;
  /** Hides the tab (e.g. a feature that is disabled on the server). */
  hidden?: boolean;
}

/** Underlined tab strip that navigates between child routes of a detail page. */
@Component({
  selector: 'app-route-tabs',
  imports: [RouterLink, RouterLinkActive, LucideDynamicIcon],
  host: { class: 'block border-b border-base-300' },
  template: `
    <nav class="-mb-px flex gap-1 overflow-x-auto" role="tablist">
      @for (tab of tabs(); track tab.link) {
        @if (!tab.hidden) {
          <a role="tab" [routerLink]="tab.link" routerLinkActive #rla="routerLinkActive"
            [attr.aria-selected]="rla.isActive"
            class="inline-flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-semibold whitespace-nowrap transition-colors"
            [class]="rla.isActive ? 'border-primary text-primary' : 'border-transparent text-base-content/60 hover:text-base-content hover:border-base-300'">
            @if (tab.icon) { <svg [lucideIcon]="tab.icon" class="size-4"></svg> }
            {{ tab.label }}
          </a>
        }
      }
    </nav>
  `
})
export class RouteTabsComponent {
  public readonly tabs = input.required<RouteTab[]>();
}
