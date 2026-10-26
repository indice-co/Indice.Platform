import { Component, Injectable, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucideArrowLeft, LucideListChecks, LucideLock, LucideSettings2, LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, IdentityResourceInfo } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** The identity resource shown by the edit page, shared with its tabs. */
@Injectable()
export class IdentityResourceStore {
  public readonly resource = signal<IdentityResourceInfo | null>(null);
  public readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  /** Whether the current user may change the resource (reserved resources are read-only). */
  public editable(): boolean {
    return this.canEdit && !this.resource()?.nonEditable;
  }

  public patch(changes: Partial<IdentityResourceInfo>): void {
    const current = this.resource();
    if (current) {
      this.resource.set(new IdentityResourceInfo({ ...current, ...changes }));
    }
  }
}

/** Identity resource page: header and tabs (details, claims). */
@Component({
  selector: 'app-identity-resource-edit',
  imports: [
    RouterLink, RouterOutlet, PageHeaderComponent, RouteTabsComponent, StatusBadgeComponent, LucideArrowLeft, LucideLock, LucideTrash
  ],
  providers: [IdentityResourceStore],
  template: `
    @if (store.resource(); as resource) {
      <app-page-header [title]="resource.displayName || resource.name!" [subtitle]="resource.description">
        <a leading routerLink="/resources/identity" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
          <svg lucideArrowLeft class="size-4"></svg>Identity resources
        </a>
        <div class="mt-2 flex flex-wrap items-center gap-2">
          <span class="code-chip">{{ resource.name }}</span>
          @if (resource.enabled) { <app-status-badge tone="success">Enabled</app-status-badge> } @else { <app-status-badge>Disabled</app-status-badge> }
          @if (resource.nonEditable) {
            <span class="badge badge-sm badge-warning badge-soft gap-1"><svg lucideLock class="size-3"></svg>Reserved</span>
          }
        </div>
        @if (store.editable()) {
          <div actions>
            <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove(resource)">
              <svg lucideTrash class="size-4"></svg>Delete
            </button>
          </div>
        }
      </app-page-header>
      <app-route-tabs class="mb-6" [tabs]="tabs" />
      <router-outlet />
    } @else {
      <div class="space-y-4">
        <div class="skeleton h-10 w-72"></div>
        <div class="skeleton h-64 w-full"></div>
      </div>
    }
  `
})
export class IdentityResourceEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly store = inject(IdentityResourceStore);

  /** Bound from the `:id` route param. */
  public readonly id = input.required<string>();

  protected readonly tabs: RouteTab[] = [
    { label: 'Details', link: 'details', icon: LucideSettings2 },
    { label: 'User claims', link: 'claims', icon: LucideListChecks }
  ];

  public ngOnInit(): void {
    this._api.getIdentityResource(+this.id()).subscribe({
      next: resource => this.store.resource.set(resource),
      error: () => this._router.navigate(['/resources/identity'])
    });
  }

  protected remove(resource: IdentityResourceInfo): void {
    this._dialog
      .confirm({
        title: 'Delete identity resource?',
        message: `Identity resource '${resource.name}' will be deleted permanently and clients will no longer be able to request it.`,
        confirmLabel: 'Delete',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteIdentityResource(resource.id!)))
      .subscribe(() => {
        this._toast.success('Identity resource deleted', `Identity resource '${resource.name}' was deleted.`);
        this._router.navigate(['/resources/identity']);
      });
  }
}
