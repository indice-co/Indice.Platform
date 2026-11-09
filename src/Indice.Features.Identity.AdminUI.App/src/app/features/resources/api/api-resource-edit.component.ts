import { Component, Injectable, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Observable, filter, switchMap, tap } from 'rxjs';
import { LucideArrowLeft, LucideKeyRound, LucideListChecks, LucideLock, LucideSettings2, LucideTrash, LucideVault } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiResourceInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** RefreshBus scope notified when an API resource changes outside its page (e.g. a scope added in the side pane). */
export const API_RESOURCE_REFRESH = 'api-resource';

/** The API resource shown by the edit page, shared with its tabs. */
@Injectable()
export class ApiResourceStore {
  private readonly _api = inject(IdentityApiService);
  public readonly resource = signal<ApiResourceInfo | null>(null);
  public readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  /** Whether the current user may change the resource. */
  public editable(): boolean {
    return this.canEdit;
  }

  /** Whether the current user may delete the resource (reserved resources cannot be deleted). */
  public deletable(): boolean {
    return this.canEdit && !this.resource()?.nonEditable;
  }

  public load(id: number): Observable<ApiResourceInfo> {
    return this._api.getApiResource(id).pipe(tap(resource => {
      resource.scopes = [...(resource.scopes ?? [])].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''));
      resource.secrets ??= [];
      resource.allowedClaims ??= [];
      this.resource.set(resource);
    }));
  }

  public patch(changes: Partial<ApiResourceInfo>): void {
    const current = this.resource();
    if (current) {
      this.resource.set(new ApiResourceInfo({ ...current, ...changes }));
    }
  }
}

/** API resource page: header and tabs (details, scopes, claims, secrets). */
@Component({
  selector: 'app-api-resource-edit',
  imports: [RouterLink, RouterOutlet, PageHeaderComponent, RouteTabsComponent, StatusBadgeComponent, TranslatePipe, LucideArrowLeft, LucideLock, LucideTrash],
  providers: [ApiResourceStore],
  template: `
    @if (store.resource(); as resource) {
      <app-page-header [title]="resource.displayName || resource.name!" [subtitle]="resource.description">
        <a leading routerLink="/resources/api" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
          <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.ApiResources' | translate }}
        </a>
        <div class="mt-2 flex flex-wrap items-center gap-2">
          <span class="code-chip">{{ resource.name }}</span>
          @if (resource.enabled) { <app-status-badge tone="success">{{ 'Common.Enabled' | translate }}</app-status-badge> } @else { <app-status-badge>{{ 'Common.Disabled' | translate }}</app-status-badge> }
          @if (resource.nonEditable) {
            <span class="badge badge-sm badge-warning badge-soft gap-1"><svg lucideLock class="size-3"></svg>{{ 'Common.Reserved' | translate }}</span>
          }
        </div>
        @if (store.deletable()) {
          <div actions>
            <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove(resource)">
              <svg lucideTrash class="size-4"></svg>{{ 'Common.Delete' | translate }}
            </button>
          </div>
        }
      </app-page-header>
      <app-route-tabs class="mb-6" [tabs]="tabs" />
      <!-- Keep the outlet inside an HTML element: as a direct child of a control flow block it can inherit the SVG namespace of a preceding icon, which makes the routed tab render as an (invisible) SVG element. -->
      <div><router-outlet /></div>
    } @else {
      <div class="space-y-4">
        <div class="skeleton h-10 w-72"></div>
        <div class="skeleton h-64 w-full"></div>
      </div>
    }
  `
})
export class ApiResourceEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(ApiResourceStore);

  /** Bound from the `:id` route param. */
  public readonly id = input.required<string>();

  protected readonly tabs: RouteTab[] = [
    { label: this._translate.instant('Common.Details'), link: 'details', icon: LucideSettings2 },
    { label: this._translate.instant('Resources.Api.Tabs.Scopes'), link: 'scopes', icon: LucideKeyRound },
    { label: this._translate.instant('Resources.UserClaims'), link: 'claims', icon: LucideListChecks },
    { label: this._translate.instant('Resources.Api.Tabs.Secrets'), link: 'secrets', icon: LucideVault }
  ];

  constructor() {
    inject(RefreshBus).on(API_RESOURCE_REFRESH).pipe(takeUntilDestroyed()).subscribe(() => this.store.load(+this.id()).subscribe());
  }

  public ngOnInit(): void {
    this.store.load(+this.id()).subscribe({ error: () => this._router.navigate(['/resources/api']) });
  }

  protected remove(resource: ApiResourceInfo): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Resources.Api.Delete.Title'),
        message: this._translate.instant('Resources.Api.Delete.Message', { name: resource.name }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteApiResource(resource.id!)))
      .subscribe(() => {
        this._toast.success(this._translate.instant('Resources.Api.Deleted.Title'), this._translate.instant('Resources.Api.Deleted.Message', { name: resource.name }));
        this._router.navigate(['/resources/api']);
      });
  }
}
