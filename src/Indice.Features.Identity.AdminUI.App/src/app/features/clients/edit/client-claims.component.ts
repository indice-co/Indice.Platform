import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideListChecks, LucidePlus, LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClaimInfo } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClientStore } from './client-store.service';

/** Claims issued for the client itself (not the user), and how they are sent. */
@Component({
  selector: 'app-client-claims',
  imports: [ReactiveFormsModule, EmptyStateComponent, LucideListChecks, LucidePlus, LucideTrash],
  template: `
    <div class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
      <div class="panel overflow-hidden">
        @if (canEdit) {
          <form class="grid gap-2 border-b border-base-300 p-4 sm:grid-cols-[1fr_1fr_auto]" [formGroup]="claimForm" (ngSubmit)="add()">
            <input class="input w-full font-mono text-sm" formControlName="type" placeholder="Claim type" aria-label="Claim type" />
            <input class="input w-full" formControlName="value" placeholder="Value" aria-label="Claim value" />
            <button type="submit" class="btn btn-primary" [disabled]="claimForm.invalid || adding()">
              @if (adding()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucidePlus class="size-4"></svg> }
              Add claim
            </button>
          </form>
        }
        <div class="overflow-x-auto">
          <table class="table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Value</th>
                @if (canEdit) { <th class="w-12"><span class="sr-only">Delete</span></th> }
              </tr>
            </thead>
            <tbody>
              @for (claim of claims(); track claim.id) {
                <tr>
                  <td><code class="code-chip">{{ claim.type }}</code></td>
                  <td class="break-all">{{ claim.value }}</td>
                  @if (canEdit) {
                    <td class="text-right">
                      <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Delete claim ' + claim.type" (click)="remove(claim)">
                        <svg lucideTrash class="size-4"></svg>
                      </button>
                    </td>
                  }
                </tr>
              } @empty {
                <tr>
                  <td [attr.colspan]="canEdit ? 3 : 2">
                    <app-empty-state title="No client claims" message="Client claims are added to tokens issued to this client.">
                      <svg icon lucideListChecks class="size-5"></svg>
                    </app-empty-state>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>

      <form class="panel h-fit space-y-4 p-6" [formGroup]="settings" (ngSubmit)="saveSettings()">
        <h2 class="font-semibold">Issuing</h2>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Claim type prefix</legend>
          <input class="input w-full font-mono text-sm" formControlName="clientClaimsPrefix" placeholder="client_" />
          <p class="label whitespace-normal">Prepended to client claim types. Leave empty for no prefix.</p>
        </fieldset>
        <label class="flex cursor-pointer items-start gap-3">
          <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" formControlName="alwaysSendClientClaims" />
          <span>
            <span class="block text-sm font-medium">Always send client claims</span>
            <span class="block text-xs text-base-content/60">Include them in every flow, not only client credentials.</span>
          </span>
        </label>
        @if (canEdit) {
          <button type="submit" class="btn btn-primary btn-sm" [disabled]="saving() || settings.pristine">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save
          </button>
        }
      </form>
    </div>
  `
})
export class ClientClaimsComponent implements OnInit {
  private readonly _store = inject(ClientStore);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _fb = inject(NonNullableFormBuilder);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly claims = computed(() => [...(this._store.client()?.claims ?? [])].sort((a, b) => (a.type ?? '').localeCompare(b.type ?? '')));
  protected readonly adding = signal(false);
  protected readonly saving = signal(false);
  protected readonly claimForm = this._fb.group({
    type: ['', Validators.required],
    value: ['', Validators.required]
  });
  protected readonly settings = this._fb.group({
    clientClaimsPrefix: [''],
    alwaysSendClientClaims: [false]
  });

  public ngOnInit(): void {
    const client = this._store.client()!;
    this.settings.setValue({ clientClaimsPrefix: client.clientClaimsPrefix ?? '', alwaysSendClientClaims: !!client.alwaysSendClientClaims });
    if (!this.canEdit) {
      this.settings.disable();
    }
  }

  protected add(): void {
    const { type, value } = this.claimForm.getRawValue();
    this.adding.set(true);
    this._store.addClaim(type.trim(), value).subscribe({
      next: () => {
        this.adding.set(false);
        this.claimForm.reset();
        this._toast.success('Claim added', `Claim '${type}' was added to the client.`);
      },
      error: () => this.adding.set(false)
    });
  }

  protected remove(claim: ClaimInfo): void {
    this._dialog
      .confirm({ title: 'Delete claim?', message: `Claim '${claim.type}' will no longer be issued to this client.`, confirmLabel: 'Delete', tone: 'danger' })
      .pipe(filter(Boolean), switchMap(() => this._store.removeClaim(claim.id!)))
      .subscribe(() => this._toast.success('Claim deleted', `Claim '${claim.type}' was removed.`));
  }

  protected saveSettings(): void {
    const value = this.settings.getRawValue();
    this.saving.set(true);
    this._store.update({ clientClaimsPrefix: value.clientClaimsPrefix || undefined, alwaysSendClientClaims: value.alwaysSendClientClaims }).subscribe({
      next: () => {
        this.saving.set(false);
        this.settings.markAsPristine();
        this._toast.success('Claim settings saved');
      },
      error: () => this.saving.set(false)
    });
  }
}
