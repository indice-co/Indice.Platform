import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideTimer } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { SingleClientInfo } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { DEVICE_CODE, KNOWN_GRANT_TYPES, formatDuration, hasAllGrants, hasAnyGrant } from '../client-utils';
import { ClientStore } from './client-store.service';

type Grants = string[];
type Key = keyof SingleClientInfo & string;

interface Field {
  key: Key;
  label: string;
  hint: string;
  when: (grants: Grants) => boolean;
}

interface LifetimeField extends Field {
  presets: [number, string][];
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
  { key: 'identityTokenLifetime', label: 'Identity token', hint: 'Lifetime of the identity token.', when: any(INTERACTIVE), required: true, presets: [[5 * MIN, '5 min'], [10 * MIN, '10 min']] },
  { key: 'accessTokenLifetime', label: 'Access token', hint: 'Lifetime of the access token.', when: any(ANY), required: true, presets: [[15 * MIN, '15 min'], [30 * MIN, '30 min'], [HOUR, '1 hr'], [5 * HOUR, '5 hr']] },
  { key: 'authorizationCodeLifetime', label: 'Authorization code', hint: 'Lifetime of the authorization code.', when: any(BROWSER), required: true, presets: [[5 * MIN, '5 min'], [10 * MIN, '10 min']] },
  { key: 'deviceCodeLifetime', label: 'Device code', hint: 'Lifetime of the device code.', when: any([DEVICE_CODE]), presets: [[5 * MIN, '5 min'], [10 * MIN, '10 min']] },
  { key: 'consentLifetime', label: 'Consent', hint: 'How long a given consent lasts. Empty means forever.', when: any(CONSENT), presets: [[7 * DAY, '1 wk'], [30 * DAY, '1 mo'], [365 * DAY, '1 yr']] },
  { key: 'userSsoLifetime', label: 'SSO session', hint: 'Maximum time since the user last authenticated. Empty means the session decides.', when: any(CONSENT), presets: [[HOUR, '1 hr'], [12 * HOUR, '12 hr'], [DAY, '1 day']] },
  { key: 'absoluteRefreshTokenLifetime', label: 'Refresh token (absolute)', hint: 'Maximum lifetime of a refresh token.', when: any(REFRESH), required: true, presets: [[DAY, '1 day'], [15 * DAY, '15 days'], [30 * DAY, '30 days']] },
  { key: 'slidingRefreshTokenLifetime', label: 'Refresh token (sliding)', hint: 'Lifetime added each time a sliding refresh token is used.', when: any(REFRESH), required: true, presets: [[DAY, '1 day'], [15 * DAY, '15 days'], [30 * DAY, '30 days']] }
];

const SELECTS: SelectField[] = [
  { key: 'accessTokenType', label: 'Access token type', hint: 'Self contained JWT, or a reference token validated through introspection.', when: any(ANY), options: [['Jwt', 'JWT'], ['Reference', 'Reference']] },
  { key: 'refreshTokenUsage', label: 'Refresh token usage', hint: 'Re-use keeps the same handle; one time only issues a new handle on every refresh.', when: any(REFRESH), options: [['ReUse', 'Re-use'], ['OneTimeOnly', 'One time only']] },
  { key: 'refreshTokenExpiration', label: 'Refresh token expiration', hint: 'Absolute expires at a fixed time; sliding renews the lifetime on every refresh.', when: any(REFRESH), options: [['Absolute', 'Absolute'], ['Sliding', 'Sliding']] }
];

const TEXTS: Field[] = [
  { key: 'frontChannelLogoutUri', label: 'Front-channel logout URI', hint: 'Logout URI at the client for HTTP front-channel logout.', when: any(BROWSER) },
  { key: 'backChannelLogoutUri', label: 'Back-channel logout URI', hint: 'Logout URI at the client for HTTP back-channel logout.', when: backChannel },
  { key: 'userCodeType', label: 'User code type', hint: 'Type of the user code generated in the device flow.', when: any([DEVICE_CODE]) },
  { key: 'pairWiseSubjectSalt', label: 'Pair-wise subject salt', hint: 'Salt for generating pair-wise subject ids for this client\'s users.', when: any(ANY) }
];

const TOGGLES: Field[] = [
  { key: 'requirePkce', label: 'Require PKCE', hint: 'A proof key is required for authorization code requests.', when: any(['authorization_code', 'hybrid']) },
  { key: 'allowPlainTextPkce', label: 'Allow plain text PKCE', hint: 'The proof key may be sent with the plain method.', when: any(['authorization_code', 'hybrid']) },
  { key: 'allowOfflineAccess', label: 'Allow offline access', hint: 'The client may request refresh tokens.', when: grants => hasAnyGrant(grants, REFRESH) || grants.some(g => !KNOWN_GRANT_TYPES.includes(g)) },
  { key: 'updateAccessTokenClaimsOnRefresh', label: 'Update claims on refresh', hint: 'Access token claims are refreshed together with the token.', when: any(REFRESH) },
  { key: 'alwaysIncludeUserClaimsInIdToken', label: 'User claims in identity token', hint: 'Always put user claims in the identity token instead of the userinfo endpoint.', when: any(INTERACTIVE) },
  { key: 'includeJwtId', label: 'Include JWT id', hint: 'JWT access tokens get a unique identifier (jti).', when: any(ANY) },
  { key: 'allowAccessTokensViaBrowser', label: 'Access tokens via browser', hint: 'Access tokens may travel through the browser.', when: any(['hybrid', 'implicit']) },
  { key: 'frontChannelLogoutSessionRequired', label: 'Front-channel logout session', hint: 'Send the session id to the front-channel logout URI.', when: any(BROWSER) },
  { key: 'backChannelLogoutSessionRequired', label: 'Back-channel logout session', hint: 'Send the session id to the back-channel logout URI.', when: backChannel }
];

/** Token lifetimes and protocol options. Which settings apply depends on the client's grant types. */
@Component({
  selector: 'app-client-tokens',
  imports: [ReactiveFormsModule, LucideTimer],
  template: `
    @if (nothingApplies()) {
      <div class="panel p-6 text-sm text-base-content/70">Token settings depend on the grant types. Add a grant type to this client first.</div>
    } @else {
      <form [formGroup]="form" (ngSubmit)="save()" class="space-y-6">
        <section class="panel p-6">
          <h2 class="mb-4 font-semibold">Lifetimes</h2>
          <div class="grid gap-x-8 gap-y-5 lg:grid-cols-2">
            @for (field of lifetimes(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label }}{{ field.required ? ' *' : '' }}</legend>
                <div class="flex flex-wrap items-center gap-2">
                  <div class="join">
                    @for (preset of field.presets; track preset[0]) {
                      <button type="button" class="btn btn-sm join-item" [disabled]="!canEdit"
                        [class.btn-primary]="form.controls[field.key].value === preset[0]" (click)="setValue(field.key, preset[0])">{{ preset[1] }}</button>
                    }
                  </div>
                  <label class="input input-sm w-36">
                    <input type="number" min="0" [formControlName]="field.key" [attr.aria-label]="field.label + ' in seconds'" />
                    <span class="text-xs opacity-60">sec</span>
                  </label>
                </div>
                <p class="label flex items-center gap-1 whitespace-normal" [class.text-error]="form.controls[field.key].invalid">
                  @if (form.controls[field.key].invalid) {
                    A value of zero or more seconds is required.
                  } @else {
                    {{ field.hint }}
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
            <h2 class="font-semibold">Token options</h2>
            @for (field of selects(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label }}</legend>
                <select class="select w-full" [formControlName]="field.key">
                  @for (option of field.options; track option[0]) { <option [value]="option[0]">{{ option[1] }}</option> }
                </select>
                <p class="label whitespace-normal">{{ field.hint }}</p>
              </fieldset>
            }
            @for (field of texts(); track field.key) {
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ field.label }}</legend>
                <input class="input w-full" [formControlName]="field.key" />
                <p class="label whitespace-normal">{{ field.hint }}</p>
              </fieldset>
            }
          </section>

          <section class="panel space-y-4 p-6">
            <h2 class="font-semibold">Behaviour</h2>
            @for (field of toggles(); track field.key) {
              <label class="flex cursor-pointer items-start gap-3">
                <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" [formControlName]="field.key" />
                <span>
                  <span class="block text-sm font-medium">{{ field.label }}</span>
                  <span class="block text-xs text-base-content/60">{{ field.hint }}</span>
                </span>
              </label>
            }
          </section>
        </div>

        @if (canEdit) {
          <div class="flex justify-end">
            <button type="submit" class="btn btn-primary" [disabled]="saving()">
              @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
              Save changes
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
    return formatDuration(seconds);
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
        this._toast.success('Token settings saved', `'${client.clientName || client.clientId}' was updated.`);
      },
      error: () => this.saving.set(false)
    });
  }
}
