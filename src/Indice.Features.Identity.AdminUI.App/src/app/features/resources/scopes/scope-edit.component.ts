import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { filter, map, switchMap } from 'rxjs';
import { LucideArrowLeft, LucideLock, LucideTrash } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { ApiScopeInfo, IdentityApiService, UpdateApiScopeRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimsAssignmentComponent } from '../shared/claims-assignment.component';
import { ScopeFormComponent } from '../shared/scope-form.component';

/** API scope page: details form and user claims side by side. */
@Component({
  selector: 'app-scope-edit',
  imports: [RouterLink, PageHeaderComponent, ScopeFormComponent, ClaimsAssignmentComponent, TranslatePipe, LucideArrowLeft, LucideLock, LucideTrash],
  template: `
    @if (scope(); as scope) {
      <app-page-header [title]="scope.displayName || scope.name!" [subtitle]="scope.description">
        <a leading routerLink="/resources/scopes" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
          <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.ApiScopes' | translate }}
        </a>
        <div class="mt-2 flex flex-wrap items-center gap-2">
          <span class="code-chip">{{ scope.name }}</span>
          @if (scope.nonEditable) {
            <span class="badge badge-sm badge-warning badge-soft gap-1"><svg lucideLock class="size-3"></svg>{{ 'Common.Reserved' | translate }}</span>
          }
        </div>
        @if (canEdit && !scope.nonEditable) {
          <div actions>
            <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove(scope)">
              <svg lucideTrash class="size-4"></svg>{{ 'Common.Delete' | translate }}
            </button>
          </div>
        }
      </app-page-header>

      <div class="space-y-6">
        <section class="panel p-6">
          <h2 class="mb-4 font-semibold">{{ 'Common.Details' | translate }}</h2>
          <app-scope-form [scope]="scope" [readonly]="!canEdit" [saving]="saving()" (save)="update(scope, $event)" />
        </section>
        <section>
          <h2 class="mb-1 font-semibold">{{ 'Resources.UserClaims' | translate }}</h2>
          <p class="mb-4 text-sm text-base-content/70">{{ 'Resources.Scopes.ClaimsIntro' | translate }}</p>
          <app-claims-assignment [assigned]="claims()" [readonly]="!canEdit" (added)="addClaim(scope, $event)" (removed)="removeClaim(scope, $event)" />
        </section>
      </div>
    } @else {
      <div class="space-y-4">
        <div class="skeleton h-10 w-72"></div>
        <div class="skeleton h-64 w-full"></div>
      </div>
    }
  `
})
export class ScopeEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  /** Bound from the `:name` route param. */
  public readonly name = input.required<string>();

  protected readonly scope = signal<ApiScopeInfo | null>(null);
  protected readonly claims = computed(() => this.scope()?.userClaims ?? []);
  protected readonly saving = signal(false);

  public ngOnInit(): void {
    // there is no endpoint for a single scope: search by name and pick the exact match
    this._api.getApiScopes(1, 100, undefined, this.name())
      .pipe(map(result => result.items?.find(scope => scope.name === this.name())))
      .subscribe({
        next: scope => {
          if (scope) {
            this.scope.set(scope);
          } else {
            this._toast.error(this._translate.instant('Resources.Scopes.NotFound.Title'), this._translate.instant('Resources.Scopes.NotFound.Message', { name: this.name() }));
            this._router.navigate(['/resources/scopes']);
          }
        },
        error: () => this._router.navigate(['/resources/scopes'])
      });
  }

  protected update(scope: ApiScopeInfo, request: UpdateApiScopeRequest): void {
    this.saving.set(true);
    this._api.updateApiScope(scope.name!, request).subscribe({
      next: () => {
        this.scope.set(new ApiScopeInfo({ ...scope, ...request, translations: request.translations }));
        this.saving.set(false);
        this._toast.success(this._translate.instant('Resources.Scopes.Updated.Title'), this._translate.instant('Resources.Scopes.Updated.Message', { name: scope.name }));
      },
      error: () => this.saving.set(false)
    });
  }

  protected addClaim(scope: ApiScopeInfo, claim: string): void {
    // the endpoint replaces the scope's claims with the given list
    const claims = [...this.claims(), claim].sort();
    this._api.addApiScopeClaims(scope.name!, claims).subscribe(() => {
      this.scope.set(new ApiScopeInfo({ ...this.scope()!, userClaims: claims }));
      this._toast.success(this._translate.instant('Resources.Claims.Added.Title'), this._translate.instant('Resources.Claims.Added.Message', { claim }));
    });
  }

  protected removeClaim(scope: ApiScopeInfo, claim: string): void {
    this._api.deleteApiScopeClaim(scope.name!, claim).subscribe(() => {
      this.scope.set(new ApiScopeInfo({ ...this.scope()!, userClaims: this.claims().filter(name => name !== claim) }));
      this._toast.success(this._translate.instant('Resources.Claims.Removed.Title'), this._translate.instant('Resources.Claims.Removed.Message', { claim }));
    });
  }

  protected remove(scope: ApiScopeInfo): void {
    this._dialog
      .confirm({
        title: this._translate.instant('Resources.Scopes.Delete.Title'),
        message: this._translate.instant('Resources.Scopes.Delete.Message', { name: scope.name }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteApiScope(scope.name!)))
      .subscribe(() => {
        this._toast.success(this._translate.instant('Resources.Scopes.Deleted.Title'), this._translate.instant('Resources.Scopes.Deleted.Message', { name: scope.name }));
        this._router.navigate(['/resources/scopes']);
      });
  }
}
