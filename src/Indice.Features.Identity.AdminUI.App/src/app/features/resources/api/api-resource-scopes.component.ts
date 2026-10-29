import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucideKeyRound, LucidePlus, LucideTrash } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ApiScopeInfo, IdentityApiService, UpdateApiScopeRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimsAssignmentComponent } from '../shared/claims-assignment.component';
import { ScopeFormComponent } from '../shared/scope-form.component';
import { ApiResourceStore } from './api-resource-edit.component';

/** Scopes tab of an API resource: one expandable panel per scope with its details and user claims. */
@Component({
  selector: 'app-api-resource-scopes',
  imports: [RouterLink, EmptyStateComponent, ClaimsAssignmentComponent, ScopeFormComponent, TranslatePipe, LucideKeyRound, LucidePlus, LucideTrash],
  template: `
    <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
      <p class="max-w-prose text-sm text-base-content/70">{{ 'Resources.Api.ScopesIntro' | translate }}</p>
      @if (store.editable()) {
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link(['resource-api-scope-add', resourceId()])">
          <svg lucidePlus class="size-4"></svg>{{ 'Resources.Api.AddScope' | translate }}
        </a>
      }
    </div>

    <div class="space-y-3">
      @for (scope of scopes(); track scope.id) {
        <div class="collapse-arrow panel collapse" [class.collapse-open]="expanded() === scope.id">
          <button type="button" class="collapse-title flex items-center gap-3 text-left" (click)="toggle(scope.id!)"
            [attr.aria-expanded]="expanded() === scope.id">
            <span class="grid size-8 shrink-0 place-items-center rounded-field bg-primary/10 text-primary">
              <svg lucideKeyRound class="size-4"></svg>
            </span>
            <span class="min-w-0 flex-1">
              <span class="block truncate font-mono text-sm font-semibold">{{ scope.name }}</span>
              <span class="block truncate text-xs text-base-content/65">{{ scope.displayName || scope.description || ('Resources.Api.NoDisplayName' | translate) }}</span>
            </span>
            <span class="badge badge-sm badge-ghost tabular">{{ 'Resources.Api.ClaimCount' | translate: { count: scope.userClaims?.length ?? 0 } }}</span>
          </button>
          <div class="collapse-content space-y-6">
            @if (expanded() === scope.id) {
              <app-scope-form [scope]="scope" [readonly]="!store.editable()" [saving]="saving() === scope.id" (save)="update(scope, $event)" />
              <div>
                <h3 class="mb-3 text-sm font-semibold">{{ 'Resources.UserClaims' | translate }}</h3>
                <app-claims-assignment [assigned]="scope.userClaims ?? []" [readonly]="!store.editable()"
                  (added)="addClaim(scope, $event)" (removed)="removeClaim(scope, $event)" />
              </div>
              @if (store.editable() && !scope.nonEditable) {
                <div class="flex justify-end border-t border-base-300 pt-4">
                  <button type="button" class="btn btn-ghost btn-sm text-error" (click)="remove(scope)">
                    <svg lucideTrash class="size-4"></svg>{{ 'Resources.Scopes.DeleteScope' | translate }}
                  </button>
                </div>
              }
            }
          </div>
        </div>
      } @empty {
        <div class="panel">
          <app-empty-state [title]="'Resources.Api.NoScopes.Title' | translate" [message]="'Resources.Api.NoScopes.Message' | translate">
            <svg icon lucideKeyRound class="size-5"></svg>
            @if (store.editable()) {
              <a actions class="btn btn-sm" [routerLink]="pane.link(['resource-api-scope-add', resourceId()])">
                <svg lucidePlus class="size-4"></svg>{{ 'Resources.Api.AddScope' | translate }}
              </a>
            }
          </app-empty-state>
        </div>
      }
    </div>
  `
})
export class ApiResourceScopesComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly store = inject(ApiResourceStore);
  protected readonly pane = inject(SidePaneService);

  protected readonly resourceId = computed(() => String(this.store.resource()?.id));
  protected readonly scopes = computed(() => this.store.resource()?.scopes ?? []);
  protected readonly expanded = signal<number | null>(null);
  protected readonly saving = signal<number | null>(null);

  protected toggle(id: number): void {
    this.expanded.update(current => (current === id ? null : id));
  }

  protected update(scope: ApiScopeInfo, request: UpdateApiScopeRequest): void {
    this.saving.set(scope.id!);
    this._api.updateApiResourceScope(this.store.resource()!.id!, scope.id!, request).subscribe({
      next: () => {
        this.replace(new ApiScopeInfo({ ...scope, ...request, translations: request.translations }));
        this.saving.set(null);
        this._toast.success(this._translate.instant('Resources.Scopes.Updated.Title'), this._translate.instant('Resources.Scopes.Updated.Message', { name: scope.name }));
      },
      error: () => this.saving.set(null)
    });
  }

  protected addClaim(scope: ApiScopeInfo, claim: string): void {
    this._api.addApiResourceScopeClaims(this.store.resource()!.id!, scope.id!, [claim]).subscribe(() => {
      this.replace(new ApiScopeInfo({ ...scope, userClaims: [...(scope.userClaims ?? []), claim].sort() }));
      this._toast.success(this._translate.instant('Resources.Claims.Added.Title'), this._translate.instant('Resources.Claims.AddedToScope', { claim, scope: scope.name }));
    });
  }

  protected removeClaim(scope: ApiScopeInfo, claim: string): void {
    this._api.deleteApiResourceScopeClaim(this.store.resource()!.id!, scope.id!, claim).subscribe(() => {
      this.replace(new ApiScopeInfo({ ...scope, userClaims: (scope.userClaims ?? []).filter(name => name !== claim) }));
      this._toast.success(this._translate.instant('Resources.Claims.Removed.Title'), this._translate.instant('Resources.Claims.RemovedFromScope', { claim, scope: scope.name }));
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
      .pipe(filter(Boolean), switchMap(() => this._api.deleteApiResourceScope(this.store.resource()!.id!, scope.id!)))
      .subscribe(() => {
        this.store.patch({ scopes: this.scopes().filter(s => s.id !== scope.id) });
        this._toast.success(this._translate.instant('Resources.Scopes.Deleted.Title'), this._translate.instant('Resources.Scopes.Deleted.Message', { name: scope.name }));
      });
  }

  private replace(scope: ApiScopeInfo): void {
    this.store.patch({ scopes: this.scopes().map(s => (s.id === scope.id ? scope : s)) });
  }
}
