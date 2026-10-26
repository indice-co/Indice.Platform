import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucidePencil, LucidePlus, LucideTags, LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClaimInfo, ClaimTypeInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { UserStore } from '../user-store';

/** All claims of the user ("additional details"). Adding and editing happen in the side pane. */
@Component({
  selector: 'app-user-claims',
  imports: [RouterLink, EmptyStateComponent, LucidePencil, LucidePlus, LucideTags, LucideTrash],
  template: `
    <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-base-content/70">Claims describe the user to applications, e.g. their name, birth date or locale.</p>
      @if (canEdit) {
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link(['user-claim', store.id()])"><svg lucidePlus class="size-4"></svg>Add claim</a>
      }
    </div>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr><th>Claim</th><th>Value</th>@if (canEdit) { <th class="w-24"><span class="sr-only">Actions</span></th> }</tr>
        </thead>
        <tbody>
          @if (!store.user()) {
            @for (row of [1, 2, 3]; track row) { <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr> }
          } @else {
            @for (claim of claims(); track claim.id) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <p class="font-medium">{{ typeOf(claim)?.displayName || claim.displayName || claim.type }}</p>
                  <p class="font-mono text-xs text-base-content/60">{{ claim.type }}</p>
                </td>
                <td class="max-w-xl break-all">{{ claim.value }}</td>
                @if (canEdit) {
                  <td class="text-right whitespace-nowrap">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['user-claim', store.id(), claim.id!])" [attr.aria-label]="'Edit ' + claim.type">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Delete ' + claim.type" (click)="remove(claim)">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state title="No claims" message="This user has no claims yet.">
                    <svg icon lucideTags class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `
})
export class UserClaimsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly store = inject(UserStore);
  protected readonly pane = inject(SidePaneService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  private readonly _types = toSignal(this.store.claimTypes$, { initialValue: [] as ClaimTypeInfo[] });
  protected readonly claims = computed(() => [...(this.store.user()?.claims ?? [])].sort((a, b) => (a.type ?? '').localeCompare(b.type ?? '')));

  protected typeOf(claim: ClaimInfo): ClaimTypeInfo | undefined {
    return this._types().find(type => type.name === claim.type);
  }

  protected remove(claim: ClaimInfo): void {
    this._dialog
      .confirm({ title: 'Delete claim?', message: `Claim '${claim.type}' will be removed from ${this.store.displayName()}.`, confirmLabel: 'Delete', tone: 'danger' })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteUserClaim(claim.id!, this.store.id())))
      .subscribe(() => {
        this.store.patch({ claims: (this.store.user()?.claims ?? []).filter(c => c.id !== claim.id) });
        this._toast.success('Claim deleted', `Claim '${claim.type}' was removed.`);
      });
  }
}
