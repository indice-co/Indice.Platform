import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, map, startWith } from 'rxjs';

interface Crumb {
  key: string;
  url: string;
}

/** Breadcrumbs built from `data.breadcrumb.title` translation keys along the primary route. */
@Component({
  selector: 'app-breadcrumbs',
  imports: [RouterLink, TranslatePipe],
  template: `
    <nav class="breadcrumbs truncate py-0 text-sm" aria-label="Breadcrumb">
      <ul>
        @for (crumb of crumbs(); track $index; let last = $last) {
          <li>
            @if (last) {
              <span class="font-medium" aria-current="page">{{ crumb.key | translate }}</span>
            } @else {
              <a class="text-base-content/65" [routerLink]="crumb.url">{{ crumb.key | translate }}</a>
            }
          </li>
        }
      </ul>
    </nav>
  `
})
export class BreadcrumbsComponent {
  private readonly _router = inject(Router);

  protected readonly crumbs = toSignal(
    this._router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      startWith(null),
      map(() => this.build(this._router.routerState.snapshot.root))
    ),
    { initialValue: [] as Crumb[] }
  );

  private build(root: ActivatedRouteSnapshot): Crumb[] {
    const crumbs: Crumb[] = [];
    const segments: string[] = [];
    let route: ActivatedRouteSnapshot | null = root;
    while (route) {
      segments.push(...route.url.map(s => s.path));
      const key: string | undefined = route.routeConfig?.data?.['breadcrumb']?.title;
      if (key && crumbs.at(-1)?.key !== key) {
        crumbs.push({ key, url: '/' + segments.join('/') });
      }
      route = route.firstChild;
    }
    return crumbs;
  }
}
