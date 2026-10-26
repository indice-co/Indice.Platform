import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideDices, LucideDownload, LucideFileBadge, LucideKeyRound, LucidePlus, LucideTrash, LucideX } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClientSecretInfo, CreateSecretRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { newSecret, saveBlob } from '../client-utils';
import { ClientStore } from './client-store.service';

type SecretKind = 'SharedSecret' | 'X509CertificateBase64';

/** Shared secrets and certificates a client authenticates with. */
@Component({
  selector: 'app-client-secrets',
  imports: [
    ReactiveFormsModule, DatePipe, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent, LucideDices, LucideDownload, LucideFileBadge,
    LucideKeyRound, LucidePlus, LucideTrash, LucideX
  ],
  template: `
    @if (canEdit) {
      @if (adding()) {
        <section class="panel mb-6 p-6">
          <div class="mb-4 flex items-start justify-between gap-4">
            <div>
              <h2 class="font-semibold">New secret</h2>
              <p class="text-sm text-base-content/60">Shared secret values cannot be read back after saving: copy them first.</p>
            </div>
            <button type="button" class="btn btn-ghost btn-sm btn-square" aria-label="Cancel" (click)="adding.set(false)"><svg lucideX class="size-4"></svg></button>
          </div>
          <div class="join mb-4">
            <button type="button" class="btn btn-sm join-item" [class.btn-primary]="kind() === 'SharedSecret'" (click)="kind.set('SharedSecret')">
              <svg lucideKeyRound class="size-4"></svg>Shared secret
            </button>
            <button type="button" class="btn btn-sm join-item" [class.btn-primary]="kind() === 'X509CertificateBase64'" (click)="kind.set('X509CertificateBase64')">
              <svg lucideFileBadge class="size-4"></svg>Certificate
            </button>
          </div>
          @if (kind() === 'SharedSecret') {
            <form class="grid gap-4 md:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,2fr)_auto] md:items-end" [formGroup]="form" (ngSubmit)="addSharedSecret()">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Value *</legend>
                <div class="join w-full">
                  <input class="input join-item w-full font-mono text-sm" formControlName="value" maxlength="4000" autocomplete="off" />
                  <app-copy-button class="join-item grid place-items-center border border-base-300 px-1" [value]="form.controls.value.value" label="secret" />
                  <button type="button" class="btn join-item" title="Generate a random value" (click)="form.controls.value.setValue(generate())">
                    <svg lucideDices class="size-4"></svg>
                  </button>
                </div>
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Expires on</legend>
                <input type="date" class="input w-full" formControlName="expiration" />
              </fieldset>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Description</legend>
                <input class="input w-full" formControlName="description" maxlength="2000" />
              </fieldset>
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || saving()">
                @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } Add
              </button>
            </form>
          } @else {
            <fieldset class="fieldset max-w-lg">
              <legend class="fieldset-legend">Public key (.cer)</legend>
              <input #file type="file" class="file-input w-full" accept=".cer,.crt,.pem" [disabled]="saving()" (change)="upload(file)" />
              <p class="label whitespace-normal">Keep the private key safe and upload only the public certificate.</p>
            </fieldset>
            @if (saving()) { <p class="flex items-center gap-2 text-sm"><span class="loading loading-spinner loading-xs"></span>Uploading…</p> }
          }
        </section>
      } @else {
        <div class="mb-4 flex justify-end">
          <button type="button" class="btn btn-primary btn-sm" (click)="adding.set(true)"><svg lucidePlus class="size-4"></svg>New secret</button>
        </div>
      }
    }

    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>Type</th>
            <th>Value</th>
            <th>Description</th>
            <th>Expires</th>
            <th class="w-24"><span class="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          @for (secret of secrets(); track secret.id) {
            <tr>
              <td class="whitespace-nowrap">
                <span class="inline-flex items-center gap-2 text-sm font-medium">
                  @if (isCertificate(secret)) { <svg lucideFileBadge class="size-4 text-secondary"></svg>Certificate } @else { <svg lucideKeyRound class="size-4 text-primary"></svg>Shared secret }
                </span>
              </td>
              <td class="max-w-xs"><span class="block truncate font-mono text-xs text-base-content/70" [title]="secret.value ?? ''">{{ secret.value || '—' }}</span></td>
              <td class="max-w-xs text-sm text-base-content/75">{{ secret.description || '—' }}</td>
              <td class="whitespace-nowrap">
                @if (secret.expiration) {
                  <span class="mr-2 text-sm tabular">{{ secret.expiration | date: 'mediumDate' }}</span>
                  @if (secret.isExpired) { <app-status-badge tone="error">Expired</app-status-badge> }
                } @else {
                  <span class="text-sm text-base-content/50">Never</span>
                }
              </td>
              <td class="text-right whitespace-nowrap">
                @if (isCertificate(secret)) {
                  <button type="button" class="btn btn-ghost btn-xs btn-square" aria-label="Download certificate" title="Download certificate" (click)="download(secret)">
                    <svg lucideDownload class="size-4"></svg>
                  </button>
                }
                @if (canEdit) {
                  <button type="button" class="btn btn-ghost btn-xs btn-square text-error" aria-label="Delete secret" (click)="remove(secret)">
                    <svg lucideTrash class="size-4"></svg>
                  </button>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="5">
                <app-empty-state title="No secrets" message="Confidential clients need a secret or certificate to request tokens.">
                  <svg icon lucideKeyRound class="size-5"></svg>
                </app-empty-state>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class ClientSecretsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _store = inject(ClientStore);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly secrets = computed(() => this._store.client()?.secrets ?? []);
  protected readonly adding = signal(false);
  protected readonly kind = signal<SecretKind>('SharedSecret');
  protected readonly saving = signal(false);
  protected readonly generate = newSecret;
  protected readonly form = inject(NonNullableFormBuilder).group({
    value: ['', [Validators.required, Validators.maxLength(4000)]],
    expiration: [''],
    description: ['', Validators.maxLength(2000)]
  });

  protected isCertificate(secret: ClientSecretInfo): boolean {
    return secret.type === 'X509CertificateBase64';
  }

  protected addSharedSecret(): void {
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._store.addSharedSecret(new CreateSecretRequest({
      value: value.value,
      expiration: value.expiration ? new Date(value.expiration) : undefined,
      description: value.description || undefined
    })).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.reset();
        this.adding.set(false);
        this._toast.success('Secret added', 'The client can now authenticate with the new secret.');
      },
      error: () => this.saving.set(false)
    });
  }

  protected upload(input: HTMLInputElement): void {
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    this.saving.set(true);
    this._store.addCertificate(file).subscribe({
      next: () => {
        this.saving.set(false);
        this.adding.set(false);
        this._toast.success('Certificate uploaded', `'${file.name}' was added to the client.`);
      },
      error: () => {
        this.saving.set(false);
        input.value = '';
      }
    });
  }

  protected download(secret: ClientSecretInfo): void {
    this._api.getCertificate(this._store.clientId(), secret.id!).subscribe(file =>
      saveBlob(file.data, file.fileName || `${this._store.clientId()}-${secret.id}.cer`));
  }

  protected remove(secret: ClientSecretInfo): void {
    this._dialog
      .confirm({
        title: 'Delete secret?',
        message: 'Applications still using this secret will no longer be able to request tokens. This cannot be undone.',
        confirmLabel: 'Delete',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._store.removeSecret(secret.id!)))
      .subscribe(() => this._toast.success('Secret deleted', 'The secret was removed from the client.'));
  }
}
