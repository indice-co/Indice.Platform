import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { LucidePencil, LucidePlus, LucideTags, LucideTrash } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
  imports: [RouterLink, TranslatePipe, EmptyStateComponent, LucidePencil, LucidePlus, LucideTags, LucideTrash],
  template: `
    <div class="mb-3 flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-base-content/70">{{ 'Users.Claims.Intro' | translate }}</p>
      @if (canEdit) {
        <a class="btn btn-primary btn-sm" [routerLink]="pane.link(['user-claim', store.id()])"><svg lucidePlus class="size-4"></svg>{{ 'Users.Claims.AddClaim' | translate }}</a>
      }
    </div>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ 'Users.Claims.Claim' | translate }}</th><th>{{ 'Common.Value' | translate }}</th>
            @if (canEdit) { <th class="w-24"><span class="sr-only">{{ 'Common.Actions' | translate }}</span></th> }
          </tr>
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
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['user-claim', store.id(), claim.id!])" [attr.aria-label]="'Users.Shared.EditItem' | translate: { name: claim.type }">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Users.Shared.DeleteItem' | translate: { name: claim.type }" (click)="remove(claim)">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="3">
                  <app-empty-state [title]="'Users.Claims.EmptyTitle' | translate" [message]="'Users.Claims.EmptyMessage' | translate">
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
  private readonly _translate = inject(TranslateService);
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
      .confirm({
        title: this._translate.instant('Users.Claims.DeleteTitle'),
        message: this._translate.instant('Users.Claims.DeleteMessage', { type: claim.type, name: this.store.displayName() }),
        confirmLabel: this._translate.instant('Common.Delete'),
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteUserClaim(claim.id!, this.store.id())))
      .subscribe(() => {
        this.store.patch({ claims: (this.store.user()?.claims ?? []).filter(c => c.id !== claim.id) });
        this._toast.success(this._translate.instant('Users.Claims.Deleted'), this._translate.instant('Users.Claims.DeletedMessage', { type: claim.type }));
      });
  }
}
