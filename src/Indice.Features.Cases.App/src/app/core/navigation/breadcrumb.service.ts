import { Injectable, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

export interface Crumb {
  /** Translation key from the route's `data.breadcrumb.title`. */
  label: string;
  url: string;
}

export interface ShellOptions {
  /** Full-bleed page without sidebar and top bar (sign-in hand-off, sign-out, error pages). */
  chromeless: boolean;
}

/**
 * Derives the breadcrumb trail and the shell options from the primary route tree. Routes declare
 * `data: { breadcrumb: { title: '<key>' } }`; `data.shell.showHeader === false` hides the shell chrome.
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  public readonly crumbs = signal<Crumb[]>([]);
  public readonly shell = signal<ShellOptions>({ chromeless: true });

  constructor(router: Router) {
    router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      const root = router.routerState.snapshot.root;
      this.crumbs.set(this.collect(root));
      this.shell.set({ chromeless: this.deepest(root).data?.['shell']?.showHeader === false });
    });
  }

  private collect(root: ActivatedRouteSnapshot): Crumb[] {
    const crumbs: Crumb[] = [];
    const segments: string[] = [];
    let route: ActivatedRouteSnapshot | null = root.firstChild;
    while (route) {
      segments.push(...route.url.map(segment => segment.path));
      const label: string | undefined = route.routeConfig?.data?.['breadcrumb']?.title;
      const url = '/' + segments.join('/');
      const previous = crumbs[crumbs.length - 1];
      if (label && previous?.label !== label) {
        crumbs.push({ label, url });
      } else if (label && previous) {
        previous.url = url;
      }
      route = route.firstChild;
    }
    return crumbs;
  }

  private deepest(root: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    let route = root;
    while (route.firstChild) {
      route = route.firstChild;
    }
    return route;
  }
}
