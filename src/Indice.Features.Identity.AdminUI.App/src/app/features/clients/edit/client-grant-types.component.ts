import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { catchError, filter, map, of, switchMap } from 'rxjs';
import { LucidePlus, LucideTrash, LucideTriangleAlert, LucideWorkflow } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { AuthService } from 'src/app/core/services/auth.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { KNOWN_GRANT_TYPES, isRecommendedCombination } from '../client-utils';
import { ClientStore } from './client-store.service';

const CUSTOM = '__custom__';

/** OAuth grant types the client may use. Options come from the identity server's discovery document. */
@Component({
  selector: 'app-client-grant-types',
  imports: [FormsModule, EmptyStateComponent, LucidePlus, LucideTrash, LucideTriangleAlert, LucideWorkflow],
  template: `
    <div class="panel overflow-hidden">
      @if (canEdit) {
        <form class="border-b border-base-300 p-4" (ngSubmit)="add()">
          <div class="flex flex-col gap-2 sm:flex-row">
            <select class="select w-full sm:max-w-sm" name="grantType" [(ngModel)]="selected" aria-label="Grant type">
              <option value="" disabled>Select a grant type…</option>
              @for (type of available(); track type) {
                <option [value]="type" [disabled]="owned().includes(type)">{{ type }}{{ owned().includes(type) ? ' (added)' : '' }}</option>
              }
              <option [value]="custom">Custom…</option>
            </select>
            @if (selected() === custom) {
              <input class="input w-full font-mono text-sm sm:max-w-xs" name="customGrant" [(ngModel)]="customName" placeholder="Custom grant type" aria-label="Custom grant type" />
            }
            <button type="submit" class="btn btn-primary" [disabled]="!candidate() || owned().includes(candidate()) || busy()">
              @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucidePlus class="size-4"></svg> }
              Add grant type
            </button>
          </div>
          @if (candidate() && !owned().includes(candidate()) && !recommended()) {
            <p class="mt-2 flex items-center gap-1.5 text-sm text-warning">
              <svg lucideTriangleAlert class="size-4"></svg>'{{ candidate() }}' is not a usual combination with the grant types this client already has.
            </p>
          }
        </form>
      }
      <ul class="divide-y divide-base-200">
        @for (type of owned(); track type) {
          <li class="flex items-center gap-3 px-4 py-3">
            <span class="grid size-8 shrink-0 place-items-center rounded-field bg-primary/10 text-primary"><svg lucideWorkflow class="size-4"></svg></span>
            <code class="code-chip">{{ type }}</code>
            @if (!isKnown(type)) { <span class="badge badge-sm badge-ghost">custom</span> }
            <span class="flex-1"></span>
            @if (canEdit) {
              <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Remove ' + type" (click)="remove(type)">
                <svg lucideTrash class="size-4"></svg>
              </button>
            }
          </li>
        } @empty {
          <li>
            <app-empty-state title="No grant types" message="Without a grant type the client cannot request any token.">
              <svg icon lucideWorkflow class="size-5"></svg>
            </app-empty-state>
          </li>
        }
      </ul>
    </div>
  `
})
export class ClientGrantTypesComponent {
  private readonly _store = inject(ClientStore);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();
  protected readonly custom = CUSTOM;

  protected readonly owned = this._store.grantTypes;
  protected readonly selected = signal('');
  protected readonly customName = signal('');
  protected readonly busy = signal(false);
  protected readonly candidate = computed(() => (this.selected() === CUSTOM ? this.customName() : this.selected()).trim());
  protected readonly recommended = computed(() => isRecommendedCombination(this.owned(), this.candidate()));
  protected readonly available = toSignal(
    inject(HttpClient).get<{ grant_types_supported?: string[] }>(`${settings.auth_settings.authority}/.well-known/openid-configuration`).pipe(
      map(document => [...new Set([...(document.grant_types_supported ?? []), 'hybrid'])].sort()),
      catchError(() => of(KNOWN_GRANT_TYPES))
    ),
    { initialValue: [] as string[] }
  );

  protected isKnown(type: string): boolean {
    return KNOWN_GRANT_TYPES.includes(type) || this.available().includes(type);
  }

  protected add(): void {
    const type = this.candidate();
    if (!type) {
      return;
    }
    this.busy.set(true);
    this._store.addGrantType(type).subscribe({
      next: () => {
        this.busy.set(false);
        this.selected.set('');
        this.customName.set('');
        this._toast.success('Grant type added', `'${type}' was added to the client.`);
      },
      error: () => this.busy.set(false)
    });
  }

  protected remove(type: string): void {
    this._dialog
      .confirm({ title: 'Remove grant type?', message: `The client will no longer be able to use '${type}'.`, confirmLabel: 'Remove', tone: 'danger' })
      .pipe(filter(Boolean), switchMap(() => this._store.removeGrantType(type)))
      .subscribe(() => this._toast.success('Grant type removed', `'${type}' was removed from the client.`));
  }
}
