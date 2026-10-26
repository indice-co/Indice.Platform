import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { concatMap, forkJoin, map, of } from 'rxjs';
import {
  LucideArrowLeft, LucideArrowRight, LucideCheck, LucideDices, LucideDynamicIcon, LucideFileBadge, LucideKeyRound, LucideTrash
} from '@lucide/angular';
import {
  ApiScopeInfo, CreateClientRequest, CreateSecretRequest, FileParameter, IdentityApiService, IdentityResourceInfo
} from 'src/app/core/services/identity-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { StepperComponent } from 'src/app/ui/stepper/stepper.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { TransferListComponent } from 'src/app/ui/transfer/transfer-list.component';
import { CLIENT_TYPES, ClientTypeOption, STEP_TITLES, URL_PATTERN, WizardStep, newGuid, newSecret } from '../client-utils';

type SecretKind = 'SharedSecret' | 'X509CertificateBase64';

interface PendingSecret {
  type: SecretKind;
  value: string;
  expiration?: Date;
  description?: string;
  /** The uploaded public key, for certificates. */
  file?: File;
}

/** Wizard that registers a new client: pick the kind of application, then walk through the steps that kind needs. */
@Component({
  selector: 'app-client-add',
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe, NgTemplateOutlet, PageHeaderComponent, StepperComponent, TransferListComponent, LucideDynamicIcon,
    LucideArrowLeft, LucideArrowRight, LucideCheck, LucideDices, LucideFileBadge, LucideKeyRound, LucideTrash
  ],
  template: `
    <app-page-header title="New client" [subtitle]="type() ? type()!.name + ' · ' + type()!.flow : 'Choose the kind of application you are registering.'">
      <a leading routerLink="/clients" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
        <svg lucideArrowLeft class="size-4"></svg>Clients
      </a>
    </app-page-header>

    @if (!type()) {
      <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        @for (option of types; track option.key) {
          <button type="button" class="panel flex gap-4 p-5 text-left transition-colors hover:border-primary/50"
            [class.border-primary]="selected()?.key === option.key" [class.ring-2]="selected()?.key === option.key"
            [class.ring-primary]="selected()?.key === option.key" (click)="selected.set(option)" (dblclick)="start(option)"
            [attr.aria-pressed]="selected()?.key === option.key">
            <span class="grid size-11 shrink-0 place-items-center rounded-field"
              [class]="selected()?.key === option.key ? 'bg-primary text-primary-content' : 'bg-primary/10 text-primary'">
              <svg [lucideIcon]="option.icon" class="size-5"></svg>
            </span>
            <span class="min-w-0">
              <span class="block font-semibold">{{ option.name }}</span>
              <span class="block text-xs font-medium text-primary">{{ option.flow }}</span>
              <span class="mt-2 block text-sm text-base-content/70">{{ option.description[0] }}</span>
            </span>
          </button>
        }
      </div>
      <div class="mt-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        @if (selected(); as option) {
          <ul class="list-inside list-disc space-y-0.5 text-sm text-base-content/75">
            @for (line of option.description; track $index) { <li>{{ line }}</li> }
          </ul>
        } @else {
          <p class="text-sm text-base-content/60">Select an application type to continue.</p>
        }
        <button type="button" class="btn btn-primary" [disabled]="!selected()" (click)="start(selected()!)">
          Get started<svg lucideArrowRight class="size-4"></svg>
        </button>
      </div>
    } @else {
      <div class="panel">
        <div class="border-b border-base-300 px-6 py-4">
          <app-stepper [steps]="stepTitles()" [current]="index()" (select)="index.set($event)" />
        </div>

        <form class="px-6 py-6" [formGroup]="form" (ngSubmit)="next()" novalidate>
          @switch (step()) {
            @case ('details') { <ng-container *ngTemplateOutlet="identityFields; context: { extended: true }" /> }
            @case ('basic') { <ng-container *ngTemplateOutlet="identityFields; context: { extended: false }" /> }
            @case ('urls') {
              <div class="grid max-w-2xl gap-4">
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Callback URL *</legend>
                  <input class="input w-full font-mono text-sm" formControlName="callbackUrl" maxlength="2000" placeholder="https://app.example.com/signin-oidc"
                    [class.input-error]="showError(form.controls.callbackUrl)" />
                  @if (showError(form.controls.callbackUrl)) {
                    <p class="label text-error">{{ form.controls.callbackUrl.hasError('required') ? 'A callback URL is required.' : 'Enter a valid URL.' }}</p>
                  } @else {
                    <p class="label whitespace-normal">Tokens are sent to this address. You can add more after the client is created.</p>
                  }
                </fieldset>
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Post-logout URL</legend>
                  <input class="input w-full font-mono text-sm" formControlName="postLogoutUrl" maxlength="2000" placeholder="https://app.example.com/signout-callback-oidc"
                    [class.input-error]="showError(form.controls.postLogoutUrl)" />
                  @if (showError(form.controls.postLogoutUrl)) {
                    <p class="label text-error">Enter a valid URL.</p>
                  } @else {
                    <p class="label whitespace-normal">Optional. Users can return here after signing out.</p>
                  }
                </fieldset>
              </div>
            }
            @case ('identity') {
              <p class="mb-4 text-sm text-base-content/70">Pick the identity information (scopes such as <code class="code-chip">openid</code> or <code class="code-chip">profile</code>) this client may request.</p>
              @if (identityResources(); as resources) {
                <app-transfer-list [available]="resources" [selected]="selectedIdentity()" [key]="byName" [label]="byName" [sublabel]="byDescription"
                  availableTitle="Available identity resources" selectedTitle="Granted" emptySelected="Grant at least one identity resource."
                  (added)="identity.update(list => [...list, $event.name!])" (removed)="identity.update(list => list.filter(x => x !== $event.name))" />
              } @else {
                <div class="skeleton h-64 w-full"></div>
              }
              @if (stepTouched() && identity().length === 0) {
                <p class="mt-3 text-sm text-error">Grant at least one identity resource, usually <code class="code-chip">openid</code>.</p>
              }
            }
            @case ('api') {
              <p class="mb-4 text-sm text-base-content/70">Pick the API scopes this client may request access tokens for.</p>
              @if (apiScopes(); as scopes) {
                <app-transfer-list [available]="scopes" [selected]="selectedApi()" [key]="byName" [label]="byName" [sublabel]="byDescription"
                  availableTitle="Available API scopes" selectedTitle="Granted"
                  (added)="api.update(list => [...list, $event.name!])" (removed)="api.update(list => list.filter(x => x !== $event.name))" />
              } @else {
                <div class="skeleton h-64 w-full"></div>
              }
            }
            @case ('secrets') { <ng-container *ngTemplateOutlet="secretsStep" /> }
            @case ('summary') { <ng-container *ngTemplateOutlet="summaryStep" /> }
          }
          <button type="submit" class="hidden" aria-hidden="true" tabindex="-1"></button>
        </form>

        <div class="flex items-center gap-2 border-t border-base-300 px-6 py-4">
          @if (index() > 0) {
            <button type="button" class="btn btn-ghost" (click)="index.set(index() - 1)" [disabled]="saving()"><svg lucideArrowLeft class="size-4"></svg>Back</button>
          } @else {
            <button type="button" class="btn btn-ghost" (click)="type.set(null)"><svg lucideArrowLeft class="size-4"></svg>Change type</button>
          }
          <span class="flex-1"></span>
          @if (step() === 'summary') {
            <button type="button" class="btn btn-primary" (click)="save()" [disabled]="saving()">
              @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideCheck class="size-4"></svg> }
              Create client
            </button>
          } @else {
            <button type="button" class="btn btn-primary" (click)="next()">
              {{ index() === steps().length - 2 ? 'Review' : 'Next' }}<svg lucideArrowRight class="size-4"></svg>
            </button>
          }
        </div>
      </div>
    }

    <ng-template #identityFields let-extended="extended">
      <div class="grid max-w-2xl gap-4" [formGroup]="form">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Client id *</legend>
          <div class="join w-full">
            <input class="input join-item w-full font-mono text-sm" formControlName="clientId" maxlength="200" placeholder="e.g. backoffice-web"
              [class.input-error]="showError(form.controls.clientId)" />
            <button type="button" class="btn join-item" (click)="form.controls.clientId.setValue(guid())" title="Generate a random id">
              <svg lucideDices class="size-4"></svg><span class="hidden sm:inline">Generate</span>
            </button>
          </div>
          @if (showError(form.controls.clientId)) {
            <p class="label text-error">A client id is required.</p>
          } @else {
            <p class="label whitespace-normal">The unique identifier applications use when they talk to the identity server.</p>
          }
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Display name *</legend>
          <input class="input w-full" formControlName="clientName" maxlength="200" placeholder="e.g. Back office"
            [class.input-error]="showError(form.controls.clientName)" />
          @if (showError(form.controls.clientName)) {
            <p class="label text-error">A display name is required.</p>
          } @else {
            <p class="label whitespace-normal">Shown to users on the consent screen.</p>
          }
        </fieldset>
        @if (extended) {
          <div class="grid gap-4 sm:grid-cols-2">
            <fieldset class="fieldset">
              <legend class="fieldset-legend">Client URL</legend>
              <input class="input w-full" formControlName="clientUrl" maxlength="2000" placeholder="https://app.example.com" />
            </fieldset>
            <fieldset class="fieldset">
              <legend class="fieldset-legend">Logo URL</legend>
              <input class="input w-full" formControlName="logoUrl" maxlength="2000" placeholder="https://app.example.com/logo.svg" />
            </fieldset>
          </div>
        }
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Description</legend>
          <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
        </fieldset>
        @if (extended) {
          <label class="flex cursor-pointer items-start gap-3">
            <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" formControlName="requireConsent" />
            <span>
              <span class="block text-sm font-medium">Require consent</span>
              <span class="block text-xs text-base-content/60">Users must approve the access this client requests before signing in.</span>
            </span>
          </label>
        }
      </div>
    </ng-template>

    <ng-template #secretsStep>
      <div class="grid gap-6 lg:grid-cols-[minmax(0,26rem)_1fr]">
        <div class="space-y-4">
          <div class="join w-full">
            <button type="button" class="btn join-item flex-1" [class.btn-primary]="secretType() === 'SharedSecret'" (click)="secretType.set('SharedSecret')">
              <svg lucideKeyRound class="size-4"></svg>Shared secret
            </button>
            <button type="button" class="btn join-item flex-1" [class.btn-primary]="secretType() === 'X509CertificateBase64'" (click)="secretType.set('X509CertificateBase64')">
              <svg lucideFileBadge class="size-4"></svg>Certificate
            </button>
          </div>
          @if (secretType() === 'SharedSecret') {
            <div class="space-y-3" [formGroup]="secretForm">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Value *</legend>
                <div class="join w-full">
                  <input class="input join-item w-full font-mono text-sm" formControlName="value" maxlength="4000" autocomplete="off" />
                  <button type="button" class="btn join-item" (click)="secretForm.controls.value.setValue(secret())" title="Generate a random value">
                    <svg lucideDices class="size-4"></svg>
                  </button>
                </div>
                <p class="label whitespace-normal">Copy it now: the value cannot be read back once the client is created.</p>
              </fieldset>
              <div class="grid gap-3 sm:grid-cols-2">
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Expires on</legend>
                  <input type="date" class="input w-full" formControlName="expiration" />
                </fieldset>
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Description</legend>
                  <input class="input w-full" formControlName="description" maxlength="2000" />
                </fieldset>
              </div>
              <button type="button" class="btn btn-sm" [disabled]="secretForm.invalid" (click)="addSharedSecret()">Add secret</button>
            </div>
          } @else {
            <fieldset class="fieldset">
              <legend class="fieldset-legend">Public key (.cer) *</legend>
              <input #certificate type="file" class="file-input w-full" accept=".cer,.crt,.pem" (change)="addCertificate(certificate)" />
              <p class="label whitespace-normal">Keep the private key safe and upload only the public certificate.</p>
            </fieldset>
            @if (readingCertificate()) { <p class="flex items-center gap-2 text-sm"><span class="loading loading-spinner loading-xs"></span>Reading certificate…</p> }
          }
        </div>
        <div>
          <h3 class="mb-2 text-sm font-semibold">Secrets to create <span class="badge badge-sm badge-ghost tabular">{{ secrets().length }}</span></h3>
          <ul class="divide-y divide-base-200 rounded-box border border-base-300">
            @for (item of secrets(); track $index) {
              <li class="flex items-start gap-3 px-4 py-3">
                <span class="mt-0.5 grid size-8 shrink-0 place-items-center rounded-field bg-base-200 text-base-content/70">
                  @if (item.type === 'SharedSecret') { <svg lucideKeyRound class="size-4"></svg> } @else { <svg lucideFileBadge class="size-4"></svg> }
                </span>
                <div class="min-w-0 flex-1 text-sm">
                  <p class="font-medium">{{ item.type === 'SharedSecret' ? 'Shared secret' : (item.file?.name ?? 'Certificate') }}</p>
                  <p class="truncate font-mono text-xs text-base-content/60">{{ item.value }}</p>
                  <p class="text-xs text-base-content/60">
                    {{ item.expiration ? 'Expires ' + (item.expiration | date: 'mediumDate') : 'Never expires' }}{{ item.description ? ' · ' + item.description : '' }}
                  </p>
                </div>
                <button type="button" class="btn btn-ghost btn-xs btn-square text-error" aria-label="Remove secret" (click)="removeSecret($index)">
                  <svg lucideTrash class="size-4"></svg>
                </button>
              </li>
            } @empty {
              <li class="px-4 py-8 text-center text-sm text-base-content/60">No secrets yet. You can also add them after the client is created.</li>
            }
          </ul>
        </div>
      </div>
    </ng-template>

    <ng-template #summaryStep>
      <dl class="grid max-w-3xl gap-x-8 gap-y-4 text-sm sm:grid-cols-[12rem_1fr]">
        <dt class="text-base-content/60">Application type</dt><dd class="font-medium">{{ type()?.name }} <span class="text-base-content/60">· {{ type()?.flow }}</span></dd>
        <dt class="text-base-content/60">Client id</dt><dd><code class="code-chip">{{ form.value.clientId }}</code></dd>
        <dt class="text-base-content/60">Display name</dt><dd class="font-medium">{{ form.value.clientName }}</dd>
        @if (form.value.description) { <dt class="text-base-content/60">Description</dt><dd>{{ form.value.description }}</dd> }
        @if (hasStep('details')) {
          <dt class="text-base-content/60">Require consent</dt><dd>{{ form.value.requireConsent ? 'Yes' : 'No' }}</dd>
          @if (form.value.clientUrl) { <dt class="text-base-content/60">Client URL</dt><dd class="break-all">{{ form.value.clientUrl }}</dd> }
          @if (form.value.logoUrl) { <dt class="text-base-content/60">Logo URL</dt><dd class="break-all">{{ form.value.logoUrl }}</dd> }
        }
        @if (hasStep('urls')) {
          <dt class="text-base-content/60">Callback URL</dt><dd class="font-mono text-xs break-all">{{ form.value.callbackUrl }}</dd>
          @if (form.value.postLogoutUrl) { <dt class="text-base-content/60">Post-logout URL</dt><dd class="font-mono text-xs break-all">{{ form.value.postLogoutUrl }}</dd> }
        }
        @if (hasStep('secrets')) {
          <dt class="text-base-content/60">Secrets</dt><dd>{{ secrets().length || 'None' }}</dd>
        }
        @if (hasStep('identity')) {
          <dt class="text-base-content/60">Identity resources</dt>
          <dd class="flex flex-wrap gap-1.5">@for (name of identity(); track name) { <span class="badge badge-sm badge-primary badge-soft">{{ name }}</span> }</dd>
        }
        <dt class="text-base-content/60">API scopes</dt>
        <dd class="flex flex-wrap gap-1.5">
          @for (name of api(); track name) { <span class="badge badge-sm badge-secondary badge-soft">{{ name }}</span> } @empty { None }
        </dd>
      </dl>
    </ng-template>
  `
})
export class ClientAddComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _toast = inject(ToastService);
  private readonly _fb = inject(NonNullableFormBuilder);

  protected readonly types = CLIENT_TYPES;
  protected readonly selected = signal<ClientTypeOption | null>(null);
  protected readonly type = signal<ClientTypeOption | null>(null);
  protected readonly index = signal(0);
  protected readonly stepTouched = signal(false);
  protected readonly saving = signal(false);
  protected readonly steps = computed<WizardStep[]>(() => this.type()?.steps ?? []);
  protected readonly step = computed(() => this.steps()[this.index()]);
  protected readonly stepTitles = computed(() => this.steps().map(step => STEP_TITLES[step]));

  protected readonly form = this._fb.group({
    clientId: ['', [Validators.required, Validators.maxLength(200)]],
    clientName: ['', [Validators.required, Validators.maxLength(200)]],
    clientUrl: ['', Validators.maxLength(2000)],
    logoUrl: ['', Validators.maxLength(2000)],
    description: ['', Validators.maxLength(1000)],
    requireConsent: [false],
    callbackUrl: ['', [Validators.maxLength(2000), Validators.pattern(URL_PATTERN)]],
    postLogoutUrl: ['', [Validators.maxLength(2000), Validators.pattern(URL_PATTERN)]]
  });
  protected readonly secretForm = this._fb.group({
    value: ['', [Validators.required, Validators.maxLength(4000)]],
    expiration: [''],
    description: ['', Validators.maxLength(2000)]
  });

  protected readonly identity = signal<string[]>([]);
  protected readonly api = signal<string[]>([]);
  protected readonly secrets = signal<PendingSecret[]>([]);
  protected readonly secretType = signal<SecretKind>('SharedSecret');
  protected readonly readingCertificate = signal(false);

  protected readonly identityResources = toSignal(this._api.getIdentityResources(1, 2147483647, 'name+', undefined).pipe(map(r => r.items ?? [])));
  protected readonly apiScopes = toSignal(this._api.getApiScopes(1, 2147483647, 'name+', undefined).pipe(map(r => r.items ?? [])));
  protected readonly selectedIdentity = computed(() => (this.identityResources() ?? []).filter(x => this.identity().includes(x.name!)));
  protected readonly selectedApi = computed(() => (this.apiScopes() ?? []).filter(x => this.api().includes(x.name!)));

  protected readonly byName = (item: IdentityResourceInfo | ApiScopeInfo) => item.name ?? '';
  protected readonly byDescription = (item: IdentityResourceInfo | ApiScopeInfo) => item.displayName || item.description;
  protected readonly guid = newGuid;
  protected readonly secret = newSecret;

  protected start(option: ClientTypeOption): void {
    this.selected.set(option);
    this.type.set(option);
    this.index.set(0);
    this.stepTouched.set(false);
    const callback = this.form.controls.callbackUrl;
    callback.setValidators(option.steps.includes('urls')
      ? [Validators.required, Validators.maxLength(2000), Validators.pattern(URL_PATTERN)]
      : [Validators.maxLength(2000), Validators.pattern(URL_PATTERN)]);
    callback.updateValueAndValidity();
    if (option.steps.includes('identity') && this.identity().length === 0) {
      this.identity.set(['openid']);
    }
  }

  protected hasStep(step: WizardStep): boolean {
    return this.steps().includes(step);
  }

  protected showError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || this.stepTouched());
  }

  protected next(): void {
    if (this.step() === 'summary') {
      return;
    }
    if (!this.isStepValid(this.step())) {
      this.stepTouched.set(true);
      return;
    }
    this.stepTouched.set(false);
    this.index.update(i => i + 1);
  }

  protected addSharedSecret(): void {
    const value = this.secretForm.getRawValue();
    this.secrets.update(list => [...list, {
      type: 'SharedSecret',
      value: value.value,
      expiration: value.expiration ? new Date(value.expiration) : undefined,
      description: value.description || undefined
    }]);
    this.secretForm.reset();
  }

  protected addCertificate(input: HTMLInputElement): void {
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    this.readingCertificate.set(true);
    const parameter: FileParameter = { data: file, fileName: file.name };
    this._api.getCertificateMetadata(parameter).subscribe({
      next: metadata => {
        this.secrets.update(list => [...list, {
          type: 'X509CertificateBase64', value: metadata.value ?? '', expiration: metadata.expiration, description: metadata.description, file
        }]);
        this.readingCertificate.set(false);
        input.value = '';
      },
      error: () => {
        this.readingCertificate.set(false);
        input.value = '';
      }
    });
  }

  protected removeSecret(index: number): void {
    this.secrets.update(list => list.filter((_, i) => i !== index));
  }

  protected save(): void {
    const value = this.form.getRawValue();
    const steps = this.steps();
    const request = new CreateClientRequest({
      clientType: this.type()!.key,
      clientId: value.clientId.trim(),
      clientName: value.clientName.trim(),
      description: value.description || undefined,
      requireConsent: steps.includes('details') ? value.requireConsent : false,
      clientUri: steps.includes('details') ? value.clientUrl || undefined : undefined,
      logoUri: steps.includes('details') ? value.logoUrl || undefined : undefined,
      redirectUri: steps.includes('urls') ? value.callbackUrl || undefined : undefined,
      postLogoutRedirectUri: steps.includes('urls') ? value.postLogoutUrl || undefined : undefined,
      identityResources: steps.includes('identity') ? this.identity() : [],
      apiResources: this.api(),
      secrets: this.secrets()
        .filter(x => x.type === 'SharedSecret')
        .map(x => new CreateSecretRequest({ value: x.value, expiration: x.expiration, description: x.description }))
    });
    const certificates = this.secrets().filter(x => x.file).map(x => x.file!);
    this.saving.set(true);
    this._api.createClient(request).pipe(
      concatMap(created => certificates.length === 0
        ? of(created)
        : forkJoin(certificates.map(file => this._api.uploadCertificate(created.clientId!, { data: file, fileName: file.name }))).pipe(map(() => created)))
    ).subscribe({
      next: created => {
        this._toast.success('Client created', `Client '${created.clientName || created.clientId}' is ready to use.`);
        this._router.navigate(['/clients', created.clientId]);
      },
      error: () => this.saving.set(false)
    });
  }

  private isStepValid(step: WizardStep): boolean {
    const controls = this.form.controls;
    const check = (...items: AbstractControl[]) => {
      items.forEach(control => control.markAsTouched());
      return items.every(control => control.valid);
    };
    switch (step) {
      case 'details': return check(controls.clientId, controls.clientName, controls.clientUrl, controls.logoUrl, controls.description);
      case 'basic': return check(controls.clientId, controls.clientName, controls.description);
      case 'urls': return check(controls.callbackUrl, controls.postLogoutUrl);
      case 'identity': return this.identity().length > 0;
      default: return true;
    }
  }
}
