import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClientTranslation, ExternalProvider, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { CLIENT_CULTURES } from '../client-utils';
import { ClientStore } from './client-store.service';
import { TranslatableFieldComponent } from './translatable-field.component';

/** General information, consent and sign in options of a client. */
@Component({
  selector: 'app-client-details',
  imports: [ReactiveFormsModule, TranslatableFieldComponent],
  template: `
    <form class="grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]" [formGroup]="form" (ngSubmit)="save()">
      <section class="panel space-y-4 p-6">
        <div>
          <h2 class="font-semibold">General</h2>
          <p class="text-sm text-base-content/60">What users see on the sign in and consent screens.</p>
        </div>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Display name *</legend>
          <app-translatable-field [control]="form.controls.clientName" [cultures]="cultures" [(culture)]="culture" [(translations)]="nameTranslations"
            [maxlength]="200" [invalid]="form.controls.clientName.invalid && form.controls.clientName.touched" />
          @if (form.controls.clientName.invalid && form.controls.clientName.touched) {
            <p class="label text-error">A display name is required.</p>
          }
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Description</legend>
          <app-translatable-field [control]="form.controls.description" [cultures]="cultures" [(culture)]="culture" [(translations)]="descriptionTranslations"
            [multiline]="true" [maxlength]="1000" />
        </fieldset>
        <div class="grid gap-4 md:grid-cols-2">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Client URL</legend>
            <input class="input w-full" formControlName="clientUri" maxlength="2000" placeholder="https://app.example.com" />
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Logo URL</legend>
            <input class="input w-full" formControlName="logoUri" maxlength="2000" placeholder="https://app.example.com/logo.svg" />
          </fieldset>
        </div>
      </section>

      <div class="space-y-6">
        <section class="panel space-y-4 p-6">
          <h2 class="font-semibold">Consent & sign in</h2>
          @for (option of switches; track option.key) {
            <label class="flex cursor-pointer items-start gap-3">
              <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" [formControlName]="option.key" />
              <span>
                <span class="block text-sm font-medium">{{ option.label }}</span>
                <span class="block text-xs text-base-content/60">{{ option.hint }}</span>
              </span>
            </label>
          }
        </section>

        @if (providers().length > 0) {
          <section class="panel space-y-3 p-6">
            <div>
              <h2 class="font-semibold">External providers</h2>
              <p class="text-xs text-base-content/60">Providers offered on this client's sign in page. Selecting all, or none, means no restriction.</p>
            </div>
            @for (provider of providers(); track provider.authenticationScheme) {
              <label class="flex cursor-pointer items-center gap-3 rounded-field border border-base-300 px-3 py-2 has-checked:border-primary has-checked:bg-primary/5">
                <input type="checkbox" class="checkbox checkbox-primary checkbox-sm" [checked]="allowed().has(provider.authenticationScheme!)"
                  [disabled]="!canEdit" (change)="toggleProvider(provider.authenticationScheme!, $any($event.target).checked)" />
                <img class="size-4" [src]="'assets/img/idps/' + provider.authenticationScheme + '.svg'" alt="" (error)="$any($event.target).hidden = true" />
                <span class="text-sm font-medium">{{ provider.displayName || provider.authenticationScheme }}</span>
              </label>
            }
          </section>
        }
      </div>

      @if (canEdit) {
        <div class="flex justify-end xl:col-span-2">
          <button type="submit" class="btn btn-primary" [disabled]="saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save changes
          </button>
        </div>
      }
    </form>
  `
})
export class ClientDetailsComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _store = inject(ClientStore);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly cultures = CLIENT_CULTURES;
  protected readonly culture = signal(CLIENT_CULTURES[0]);
  protected readonly nameTranslations = signal<Record<string, string>>({});
  protected readonly descriptionTranslations = signal<Record<string, string>>({});
  protected readonly saving = signal(false);
  protected readonly providers = signal<ExternalProvider[]>([]);
  protected readonly allowed = signal(new Set<string>());
  protected readonly restrictions = computed(() => {
    const all = this.providers().map(p => p.authenticationScheme!);
    const selected = all.filter(scheme => this.allowed().has(scheme));
    return selected.length > 0 && selected.length < all.length ? selected : undefined;
  });

  protected readonly switches = [
    { key: 'requireConsent', label: 'Require consent', hint: 'Users approve the requested access before signing in.' },
    { key: 'allowRememberConsent', label: 'Remember consent', hint: 'A given consent is remembered for later sign ins.' },
    { key: 'enableLocalLogin', label: 'Local accounts', hint: 'Users can sign in with a username and password.' }
  ] as const;

  protected readonly form = inject(NonNullableFormBuilder).group({
    clientName: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(1000)],
    clientUri: ['', Validators.maxLength(2000)],
    logoUri: ['', Validators.maxLength(2000)],
    requireConsent: [false],
    allowRememberConsent: [false],
    enableLocalLogin: [false]
  });

  public ngOnInit(): void {
    const client = this._store.client()!;
    this.form.setValue({
      clientName: client.clientName ?? '',
      description: client.description ?? '',
      clientUri: client.clientUri ?? '',
      logoUri: client.logoUri ?? '',
      requireConsent: !!client.requireConsent,
      allowRememberConsent: !!client.allowRememberConsent,
      enableLocalLogin: !!client.enableLocalLogin
    });
    const names: Record<string, string> = {};
    const descriptions: Record<string, string> = {};
    for (const [culture, translation] of Object.entries(client.translations ?? {})) {
      names[culture] = translation?.clientName ?? '';
      descriptions[culture] = translation?.description ?? '';
    }
    this.nameTranslations.set(names);
    this.descriptionTranslations.set(descriptions);
    if (!this.canEdit) {
      this.form.disable();
    }
    this._api.getExternalProviders().subscribe(result => {
      const providers = result.items ?? [];
      const restrictions = client.identityProviderRestrictions ?? [];
      this.providers.set(providers);
      this.allowed.set(new Set(providers.map(p => p.authenticationScheme!).filter(scheme => restrictions.length === 0 || restrictions.includes(scheme))));
    });
  }

  protected toggleProvider(scheme: string, checked: boolean): void {
    this.allowed.update(set => {
      const next = new Set(set);
      if (checked) { next.add(scheme); } else { next.delete(scheme); }
      return next;
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.culture.set(this.cultures[0]);
      return;
    }
    const value = this.form.getRawValue();
    // keep translations of cultures this screen does not offer
    const translations: { [key: string]: ClientTranslation } = { ...(this._store.client()?.translations ?? {}) };
    for (const culture of this.cultures.slice(1)) {
      delete translations[culture];
      const clientName = this.nameTranslations()[culture];
      const description = this.descriptionTranslations()[culture];
      if (clientName || description) {
        translations[culture] = new ClientTranslation({ clientName: clientName || undefined, description: description || undefined });
      }
    }
    this.saving.set(true);
    this._store.update({
      clientName: value.clientName.trim(),
      description: value.description || undefined,
      clientUri: value.clientUri || undefined,
      logoUri: value.logoUri || undefined,
      requireConsent: value.requireConsent,
      allowRememberConsent: value.allowRememberConsent,
      enableLocalLogin: value.enableLocalLogin,
      translations,
      identityProviderRestrictions: this.restrictions()
    }).subscribe({
      next: client => {
        this.saving.set(false);
        this._toast.success('Client updated', `'${client.clientName}' was saved.`);
      },
      error: () => this.saving.set(false)
    });
  }
}
