import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideTimer } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { DEVICE_CODE, KNOWN_GRANT_TYPES, formatDuration, hasAllGrants, hasAnyGrant } from '../client-utils';
import { ClientStore } from './client-store.service';

type Grants = string[];
type Key = keyof SingleClientInfo & string;

/** `label`, `hint` and option/preset labels hold translation keys. */
interface Field {
  key: Key;
  label: string;
  hint: string;
  when: (grants: Grants) => boolean;
}

interface LifetimeField extends Field {
  /** Seconds, a `Clients.Duration.*` key and the count it is formatted with. */
  presets: [number, string, number][];
  required?: boolean;
}

interface SelectField extends Field {
  options: [string, string][];
}

const MIN = 60;
const HOUR = 3600;
const DAY = 86400;
const INTERACTIVE = ['authorization_code', 'password', 'hybrid', 'implicit', DEVICE_CODE];
const ANY = [...INTERACTIVE, 'client_credentials'];
const REFRESH = ['password', 'hybrid', DEVICE_CODE, 'authorization_code', 'implicit'];
const BROWSER = ['authorization_code', 'hybrid', 'implicit'];
const CONSENT = [...BROWSER, DEVICE_CODE];
const any = (types: string[]) => (grants: Grants) => hasAnyGrant(grants, types);
const backChannel = (grants: Grants) => hasAnyGrant(grants, ['hybrid']) || hasAllGrants(grants, ['authorization_code', 'client_credentials']);

const LIFETIMES: LifetimeField[] = [
  { key: 'identityTokenLifetime', label: 'Clients.Tokens.IdentityToken', hint: 'Clients.Tokens.IdentityTokenHint', when: any(INTERACTIVE), required: true, presets: [[5 * MIN, 'Clients.Duration.Minute', 5], [10 * MIN, 'Clients.Duration.Minute', 10]] },
  { key: 'accessTokenLifetime', label: 'Clients.Tokens.AccessToken', hint: 'Clients.Tokens.AccessTokenHint', when: any(ANY), required: true, presets: [[15 * MIN, 'Clients.Duration.Minute', 15], [30 * MIN, 'Clients.Duration.Minute', 30], [HOUR, 'Clients.Duration.Hour', 1], [5 * HOUR, 'Clients.Duration.Hour', 5]] },
  { key: 'authorizationCodeLifetime', label: 'Clients.Tokens.AuthorizationCode', hint: 'Clients.Tokens.AuthorizationCodeHint', when: any(BROWSER), required: true, presets: [[5 * MIN, 'Clients.Duration.Minute', 5], [10 * MIN, 'Clients.Duration.Minute', 10]] },
  { key: 'deviceCodeLifetime', label: 'Clients.Tokens.DeviceCode', hint: 'Clients.Tokens.DeviceCodeHint', when: any([DEVICE_CODE]), presets: [[5 * MIN, 'Clients.Duration.Minute', 5], [10 * MIN, 'Clients.Duration.Minute', 10]] },
  { key: 'consentLifetime', label: 'Clients.Tokens.Consent', hint: 'Clients.Tokens.ConsentHint', when: any(CONSENT), presets: [[7 * DAY, 'Clients.Duration.Week', 1], [30 * DAY, 'Clients.Duration.Month', 1], [365 * DAY, 'Clients.Duration.Year', 1]] },
  { key: 'userSsoLifetime', label: 'Clients.Tokens.SsoSession', hint: 'Clients.Tokens.SsoSessionHint', when: any(CONSENT), presets: [[HOUR, 'Clients.Duration.Hour', 1], [12 * HOUR, 'Clients.Duration.Hour', 12], [DAY, 'Clients.Duration.Day', 1]] },
  { key: 'absoluteRefreshTokenLifetime', label: 'Clients.Tokens.RefreshTokenAbsolute', hint: 'Clients.Tokens.RefreshTokenAbsoluteHint', when: any(REFRESH), required: true, presets: [[DAY, 'Clients.Duration.Day', 1], [15 * DAY, 'Clients.Duration.Days', 15], [30 * DAY, 'Clients.Duration.Days', 30]] },
  { key: 'slidingRefreshTokenLifetime', label: 'Clients.Tokens.RefreshTokenSliding', hint: 'Clients.Tokens.RefreshTokenSlidingHint', when: any(REFRESH), required: true, presets: [[DAY, 'Clients.Duration.Day', 1], [15 * DAY, 'Clients.Duration.Days', 15], [30 * DAY, 'Clients.Duration.Days', 30]] }
];

const SELECTS: SelectField[] = [
  { key: 'accessTokenType', label: 'Clients.Tokens.AccessTokenType', hint: 'Clients.Tokens.AccessTokenTypeHint', when: any(ANY), options: [['Jwt', 'Clients.Tokens.Jwt'], ['Reference', 'Clients.Tokens.Reference']] },
  { key: 'refreshTokenUsage', label: 'Clients.Tokens.RefreshTokenUsage', hint: 'Clients.Tokens.RefreshTokenUsageHint', when: any(REFRESH), options: [['ReUse', 'Clients.Tokens.ReUse'], ['OneTimeOnly', 'Clients.Tokens.OneTimeOnly']] },
  { key: 'refreshTokenExpiration', label: 'Clients.Tokens.RefreshTokenExpiration', hint: 'Clients.Tokens.RefreshTokenExpirationHint', when: any(REFRESH), options: [['Absolute', 'Clients.Tokens.Absolute'], ['Sliding', 'Clients.Tokens.Sliding']] }
];

const TEXTS: Field[] = [
  { key: 'frontChannelLogoutUri', label: 'Clients.Tokens.FrontChannelLogoutUri', hint: 'Clients.Tokens.FrontChannelLogoutUriHint', when: any(BROWSER) },
  { key: 'backChannelLogoutUri', label: 'Clients.Tokens.BackChannelLogoutUri', hint: 'Clients.Tokens.BackChannelLogoutUriHint', when: backChannel },
  { key: 'userCodeType', label: 'Clients.Tokens.UserCodeType', hint: 'Clients.Tokens.UserCodeTypeHint', when: any([DEVICE_CODE]) },
  { key: 'pairWiseSubjectSalt', label: 'Clients.Tokens.PairWiseSubjectSalt', hint: 'Clients.Tokens.PairWiseSubjectSaltHint', when: any(ANY) }
];

const TOGGLES: Field[] = [
  { key: 'requirePkce', label: 'Clients.Tokens.RequirePkce', hint: 'Clients.Tokens.RequirePkceHint', when: any(['authorization_code', 'hybrid']) },
  { key: 'allowPlainTextPkce', label: 'Clients.Tokens.AllowPlainTextPkce', hint: 'Clients.Tokens.AllowPlainTextPkceHint', when: any(['authorization_code', 'hybrid']) },
  { key: 'allowOfflineAccess', label: 'Clients.Tokens.AllowOfflineAccess', hint: 'Clients.Tokens.AllowOfflineAccessHint', when: grants => hasAnyGrant(grants, REFRESH) || grants.some(g => !KNOWN_GRANT_TYPES.includes(g)) },
  { key: 'updateAccessTokenClaimsOnRefresh', label: 'Clients.Tokens.UpdateClaimsOnRefresh', hint: 'Clients.Tokens.UpdateClaimsOnRefreshHint', when: any(REFRESH) },
  { key: 'alwaysIncludeUserClaimsInIdToken', label: 'Clients.Tokens.UserClaimsInIdToken', hint: 'Clients.Tokens.UserClaimsInIdTokenHint', when: any(INTERACTIVE) },
  { key: 'includeJwtId', label: 'Clients.Tokens.IncludeJwtId', hint: 'Clients.Tokens.IncludeJwtIdHint', when: any(ANY) },
  { key: 'allowAccessTokensViaBrowser', label: 'Clients.Tokens.AccessTokensViaBrowser', hint: 'Clients.Tokens.AccessTokensViaBrowserHint', when: any(['hybrid', 'implicit']) },
  { key: 'frontChannelLogoutSessionRequired', label: 'Clients.Tokens.FrontChannelLogoutSession', hint: 'Clients.Tokens.FrontChannelLogoutSessionHint', when: any(BROWSER) },
  { key: 'backChannelLogoutSessionRequired', label: 'Clients.Tokens.BackChannelLogoutSession', hint: 'Clients.Tokens.BackChannelLogoutSessionHint', when: backChannel }
];

/** Token lifetimes and protocol options. Which settings apply depends on the client's grant types. */
@Component({
  selector: 'app-client-tokens',
  imports: [ReactiveFormsModule, TranslatePipe, LucideTimer],
  template: `
    @if (nothingApplies()) {
      <div class="panel p-6 text-sm text-base-content/70">{{ 'Clients.Tokens.NothingApplies' | translate }}</div>
    } @else {
      <form [formGroup]="form" (ngSubmit)="save()" class="space-y-6">
        <section class="panel p-6">
          <h2 class="mb-4 font-semibold">{{ 'Clients.Tokens.Lifetimes' | translate }}</h2>
          <div class="grid gap-x-8 gap-y-5 lg:grid-cols-2">
            @for (field of lifetimes(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label | translate }}{{ field.required ? ' *' : '' }}</legend>
                <div class="flex flex-wrap items-center gap-2">
                  <div class="join">
                    @for (preset of field.presets; track preset[0]) {
                      <button type="button" class="btn btn-sm join-item" [disabled]="!canEdit"
                        [class.btn-primary]="form.controls[field.key].value === preset[0]" (click)="setValue(field.key, preset[0])">{{ preset[1] | translate: { count: preset[2] } }}</button>
                    }
                  </div>
                  <label class="input input-sm w-36">
                    <input type="number" min="0" [formControlName]="field.key" [attr.aria-label]="'Clients.Tokens.InSeconds' | translate: { label: (field.label | translate) }" />
                    <span class="text-xs opacity-60">{{ 'Clients.Tokens.SecondsUnit' | translate }}</span>
                  </label>
                </div>
                <p class="label flex items-center gap-1 whitespace-normal" [class.text-error]="form.controls[field.key].invalid">
                  @if (form.controls[field.key].invalid) {
                    {{ 'Clients.Tokens.LifetimeInvalid' | translate }}
                  } @else {
                    {{ field.hint | translate }}
                    @if (duration(form.controls[field.key].value); as text) {
                      <span class="ml-1 inline-flex items-center gap-1 font-medium text-base-content/70"><svg lucideTimer class="size-3"></svg>{{ text }}</span>
                    }
                  }
                </p>
              </fieldset>
            }
          </div>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          <section class="panel space-y-4 p-6">
            <h2 class="font-semibold">{{ 'Clients.Tokens.TokenOptions' | translate }}</h2>
            @for (field of selects(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label | translate }}</legend>
                <select class="select w-full" [formControlName]="field.key">
                  @for (option of field.options; track option[0]) { <option [value]="option[0]">{{ option[1] | translate }}</option> }
                </select>
                <p class="label whitespace-normal">{{ field.hint | translate }}</p>
              </fieldset>
            }
            @for (field of texts(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label | translate }}</legend>
                <input class="input w-full" [formControlName]="field.key" />
                <p class="label whitespace-normal">{{ field.hint | translate }}</p>
              </fieldset>
            }
          </section>

          <section class="panel space-y-4 p-6">
            <h2 class="font-semibold">{{ 'Clients.Tokens.Behaviour' | translate }}</h2>
            @for (field of toggles(); track field.key) {
              <label class="flex cursor-pointer items-start gap-3">
                <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" [formControlName]="field.key" />
                <span>
                  <span class="block text-sm font-medium">{{ field.label | translate }}</span>
                  <span class="block text-xs text-base-content/60">{{ field.hint | translate }}</span>
                </span>
              </label>
            }
          </section>
        </div>

        @if (canEdit) {
          <div class="flex justify-end">
            <button type="submit" class="btn btn-primary" [disabled]="saving()">
              @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
              {{ 'Common.SaveChanges' | translate }}
            </button>
          </div>
        }
      </form>
    }
  `
})
export class ClientTokensComponent implements OnInit {
  private readonly _store = inject(ClientStore);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();
  protected readonly saving = signal(false);

  protected readonly lifetimes = computed(() => LIFETIMES.filter(f => f.when(this._store.grantTypes())));
  protected readonly selects = computed(() => SELECTS.filter(f => f.when(this._store.grantTypes())));
  protected readonly texts = computed(() => TEXTS.filter(f => f.when(this._store.grantTypes())));
  protected readonly toggles = computed(() => TOGGLES.filter(f => f.when(this._store.grantTypes())));
  protected readonly nothingApplies = computed(() =>
    this.lifetimes().length + this.selects().length + this.texts().length + this.toggles().length === 0);

  protected readonly form = new FormGroup<Record<string, FormControl<any>>>({});

  public ngOnInit(): void {
    const client = this._store.client()!;
    for (const field of LIFETIMES) {
      const validators = [Validators.min(0), ...(field.required && field.when(client.grantTypes ?? []) ? [Validators.required] : [])];
      this.form.addControl(field.key, new FormControl(client[field.key] ?? null, validators));
    }
    for (const field of [...SELECTS, ...TEXTS]) {
      this.form.addControl(field.key, new FormControl(client[field.key] ?? ''));
    }
    for (const field of TOGGLES) {
      this.form.addControl(field.key, new FormControl(!!client[field.key]));
    }
    if (!this.canEdit) {
      this.form.disable();
    }
  }

  protected setValue(key: string, seconds: number): void {
    this.form.controls[key].setValue(seconds);
    this.form.controls[key].markAsDirty();
  }

  protected duration(seconds: number | null): string {
    return formatDuration(seconds, (key, count) => this._translate.instant(key, { count }));
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const changes: Partial<SingleClientInfo> = {};
    for (const field of LIFETIMES) {
      const raw = value[field.key];
      (changes as any)[field.key] = raw === null || raw === '' ? undefined : +raw;
    }
    for (const field of [...SELECTS, ...TEXTS]) {
      (changes as any)[field.key] = value[field.key] || undefined;
    }
    for (const field of TOGGLES) {
      (changes as any)[field.key] = !!value[field.key];
    }
    this.saving.set(true);
    this._store.update(changes).subscribe({
      next: client => {
        this.saving.set(false);
        this.form.markAsPristine();
        this._toast.success(this._translate.instant('Clients.Tokens.SavedTitle'),
          this._translate.instant('Clients.Tokens.SavedMessage', { name: client.clientName || client.clientId }));
      },
      error: () => this.saving.set(false)
    });
  }
}
