import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { BreadcrumbService } from 'src/app/core/navigation/breadcrumb.service';
import { ToastOutletComponent } from '../feedback/toast-outlet.component';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';

/**
 * The application frame: sidebar (a drawer below `lg`) and top bar on one tint, the routed page on the sheet they hold,
 * and the `rightpane` side sheet.
 * The primary outlet never moves in the DOM, so switching between chromeless pages and the app does not re-create it.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, TranslatePipe, SidebarComponent, TopbarComponent, ToastOutletComponent],
  template: `
    <a href="#main" class="btn btn-primary btn-sm fixed top-2 left-2 z-[70] -translate-y-16 focus:translate-y-0">{{ 'shell.skipToContent' | translate }}</a>
    <div [class]="chromeless() ? 'drawer' : 'drawer lg:drawer-open'">
      <input id="cases-nav" type="checkbox" class="drawer-toggle" [checked]="navOpen()" (change)="navOpen.set($any($event.target).checked)" />
      <div class="drawer-content flex min-h-dvh min-w-0 flex-col">
        @if (!chromeless()) {
          <app-topbar (menu)="navOpen.set(true)" />
          <div class="app-sheet-cap" aria-hidden="true"></div>
        }
        <main id="main" class="min-w-0 flex-1" [class.app-sheet]="!chromeless()" tabindex="-1">
          <router-outlet />
        </main>
      </div>
      @if (!chromeless()) {
        <div class="drawer-side z-40">
          <label for="cases-nav" class="drawer-overlay" [attr.aria-label]="'shell.closeMenu' | translate"></label>
          <app-sidebar (navigate)="navOpen.set(false)" />
        </div>
      }
    </div>

    <div class="fixed inset-0 z-50 overflow-hidden transition-[visibility] duration-200" [class.invisible]="!paneOpen()" [attr.aria-hidden]="!paneOpen()">
      <div class="absolute inset-0 bg-neutral/35 transition-opacity duration-200" [class.opacity-0]="!paneOpen()" (click)="closePane()"></div>
      <!-- The routed page element is stretched so the pane's footer sits at the bottom. -->
      <aside class="absolute inset-y-0 right-0 flex w-full max-w-[var(--cases-sidepane-width)] flex-col border-l border-base-300 bg-base-100 shadow-2xl transition-transform duration-200 ease-out [&>:not(router-outlet)]:flex [&>:not(router-outlet)]:min-h-0 [&>:not(router-outlet)]:flex-1 [&>:not(router-outlet)]:flex-col"
             [class.translate-x-full]="!paneOpen()">
        <router-outlet name="rightpane" (activate)="paneOpen.set(true)" (deactivate)="paneOpen.set(false)" />
      </aside>
    </div>

    <app-toast-outlet />
  `
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly breadcrumbs = inject(BreadcrumbService);
  protected readonly chromeless = computed(() => this.breadcrumbs.shell().chromeless);
  protected readonly navOpen = signal(false);
  protected readonly paneOpen = signal(false);

  protected closePane(): void {
    this.router.navigate([{ outlets: { rightpane: null } }], { queryParamsHandling: 'preserve' });
  }
}
