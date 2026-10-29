import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
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
    ReactiveFormsModule, RouterLink, DatePipe, NgTemplateOutlet, TranslatePipe, PageHeaderComponent, StepperComponent, TransferListComponent, LucideDynamicIcon,
    LucideArrowLeft, LucideArrowRight, LucideCheck, LucideDices, LucideFileBadge, LucideKeyRound, LucideTrash
  ],
  template: `
    <app-page-header [title]="'Routes.NewClient' | translate"
      [subtitle]="type() ? (type()!.name | translate) + ' · ' + (type()!.flow | translate) : ('Clients.Add.ChooseType' | translate)">
      <a leading routerLink="/clients" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
        <svg lucideArrowLeft class="size-4"></svg>{{ 'Routes.Clients' | translate }}
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
              <span class="block font-semibold">{{ option.name | translate }}</span>
              <span class="block text-xs font-medium text-primary">{{ option.flow | translate }}</span>
              <span class="mt-2 block text-sm text-base-content/70">{{ option.description[0] | translate }}</span>
            </span>
          </button>
        }
      </div>
      <div class="mt-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        @if (selected(); as option) {
          <ul class="list-inside list-disc space-y-0.5 text-sm text-base-content/75">
            @for (line of option.description; track $index) { <li>{{ line | translate }}</li> }
          </ul>
        } @else {
          <p class="text-sm text-base-content/60">{{ 'Clients.Add.SelectTypeHint' | translate }}</p>
        }
        <button type="button" class="btn btn-primary" [disabled]="!selected()" (click)="start(selected()!)">
          {{ 'Clients.Add.GetStarted' | translate }}<svg lucideArrowRight class="size-4"></svg>
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
                  <legend class="fieldset-legend">{{ 'Clients.Fields.CallbackUrl' | translate }} *</legend>
                  <input class="input w-full font-mono text-sm" formControlName="callbackUrl" maxlength="2000" placeholder="https://app.example.com/signin-oidc"
                    [class.input-error]="showError(form.controls.callbackUrl)" />
                  @if (showError(form.controls.callbackUrl)) {
                    <p class="label text-error">{{ (form.controls.callbackUrl.hasError('required') ? 'Clients.Validation.CallbackUrlRequired' : 'Clients.Validation.InvalidUrl') | translate }}</p>
                  } @else {
                    <p class="label whitespace-normal">{{ 'Clients.Add.CallbackUrlHint' | translate }}</p>
                  }
                </fieldset>
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">{{ 'Clients.Fields.PostLogoutUrl' | translate }}</legend>
                  <input class="input w-full font-mono text-sm" formControlName="postLogoutUrl" maxlength="2000" placeholder="https://app.example.com/signout-callback-oidc"
                    [class.input-error]="showError(form.controls.postLogoutUrl)" />
                  @if (showError(form.controls.postLogoutUrl)) {
                    <p class="label text-error">{{ 'Clients.Validation.InvalidUrl' | translate }}</p>
                  } @else {
                    <p class="label whitespace-normal">{{ 'Clients.Add.PostLogoutUrlHint' | translate }}</p>
                  }
                </fieldset>
              </div>
            }
            @case ('identity') {
              <p class="mb-4 text-sm text-base-content/70" [innerHTML]="'Clients.Add.IdentityIntro' | translate"></p>
              @if (identityResources(); as resources) {
                <app-transfer-list [available]="resources" [selected]="selectedIdentity()" [key]="byName" [label]="byName" [sublabel]="byDescription"
                  [availableTitle]="'Clients.Resources.AvailableIdentity' | translate" [selectedTitle]="'Clients.Add.Granted' | translate"
                  [emptySelected]="'Clients.Add.GrantAtLeastOne' | translate"
                  (added)="identity.update(list => [...list, $event.name!])" (removed)="identity.update(list => list.filter(x => x !== $event.name))" />
              } @else {
                <div class="skeleton h-64 w-full"></div>
              }
              @if (stepTouched() && identity().length === 0) {
                <p class="mt-3 text-sm text-error" [innerHTML]="'Clients.Add.GrantAtLeastOneOpenId' | translate"></p>
              }
            }
            @case ('api') {
              <p class="mb-4 text-sm text-base-content/70">{{ 'Clients.Add.ApiIntro' | translate }}</p>
              @if (apiScopes(); as scopes) {
                <app-transfer-list [available]="scopes" [selected]="selectedApi()" [key]="byName" [label]="byName" [sublabel]="byDescription"
                  [availableTitle]="'Clients.Resources.AvailableApi' | translate" [selectedTitle]="'Clients.Add.Granted' | translate"
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
            <button type="button" class="btn btn-ghost" (click)="index.set(index() - 1)" [disabled]="saving()"><svg lucideArrowLeft class="size-4"></svg>{{ 'Common.Back' | translate }}</button>
          } @else {
            <button type="button" class="btn btn-ghost" (click)="type.set(null)"><svg lucideArrowLeft class="size-4"></svg>{{ 'Clients.Add.ChangeType' | translate }}</button>
          }
          <span class="flex-1"></span>
          @if (step() === 'summary') {
            <button type="button" class="btn btn-primary" (click)="save()" [disabled]="saving()">
              @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideCheck class="size-4"></svg> }
              {{ 'Clients.Add.CreateClient' | translate }}
            </button>
          } @else {
            <button type="button" class="btn btn-primary" (click)="next()">
              {{ (index() === steps().length - 2 ? 'Clients.Add.Review' : 'Common.Next') | translate }}<svg lucideArrowRight class="size-4"></svg>
            </button>
          }
        </div>
      </div>
    }

    <ng-template #identityFields let-extended="extended">
      <div class="grid max-w-2xl gap-4" [formGroup]="form">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Clients.Fields.ClientId' | translate }} *</legend>
          <div class="join w-full">
            <input class="input join-item w-full font-mono text-sm" formControlName="clientId" maxlength="200" [placeholder]="'Clients.Add.ClientIdPlaceholder' | translate"
              [class.input-error]="showError(form.controls.clientId)" />
            <button type="button" class="btn join-item" (click)="form.controls.clientId.setValue(guid())" [title]="'Clients.Add.GenerateId' | translate">
              <svg lucideDices class="size-4"></svg><span class="hidden sm:inline">{{ 'Clients.Add.Generate' | translate }}</span>
            </button>
          </div>
          @if (showError(form.controls.clientId)) {
            <p class="label text-error">{{ 'Clients.Validation.ClientIdRequired' | translate }}</p>
          } @else {
            <p class="label whitespace-normal">{{ 'Clients.Add.ClientIdHint' | translate }}</p>
          }
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.DisplayName' | translate }} *</legend>
          <input class="input w-full" formControlName="clientName" maxlength="200" [placeholder]="'Clients.Add.DisplayNamePlaceholder' | translate"
            [class.input-error]="showError(form.controls.clientName)" />
          @if (showError(form.controls.clientName)) {
            <p class="label text-error">{{ 'Clients.Validation.DisplayNameRequired' | translate }}</p>
          } @else {
            <p class="label whitespace-normal">{{ 'Clients.Add.DisplayNameHint' | translate }}</p>
          }
        </fieldset>
        @if (extended) {
          <div class="grid gap-4 sm:grid-cols-2">
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ 'Clients.Fields.ClientUrl' | translate }}</legend>
              <input class="input w-full" formControlName="clientUrl" maxlength="2000" placeholder="https://app.example.com" />
            </fieldset>
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ 'Clients.Fields.LogoUrl' | translate }}</legend>
              <input class="input w-full" formControlName="logoUrl" maxlength="2000" placeholder="https://app.example.com/logo.svg" />
            </fieldset>
          </div>
        }
        <fieldset class="fieldset">
          <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
          <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
        </fieldset>
        @if (extended) {
          <label class="flex cursor-pointer items-start gap-3">
            <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" formControlName="requireConsent" />
            <span>
              <span class="block text-sm font-medium">{{ 'Clients.Fields.RequireConsent' | translate }}</span>
              <span class="block text-xs text-base-content/60">{{ 'Clients.Add.RequireConsentHint' | translate }}</span>
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
              <svg lucideKeyRound class="size-4"></svg>{{ 'Clients.Secrets.SharedSecret' | translate }}
            </button>
            <button type="button" class="btn join-item flex-1" [class.btn-primary]="secretType() === 'X509CertificateBase64'" (click)="secretType.set('X509CertificateBase64')">
              <svg lucideFileBadge class="size-4"></svg>{{ 'Clients.Secrets.Certificate' | translate }}
            </button>
          </div>
          @if (secretType() === 'SharedSecret') {
            <div class="space-y-3" [formGroup]="secretForm">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">{{ 'Common.Value' | translate }} *</legend>
                <div class="join w-full">
                  <input class="input join-item w-full font-mono text-sm" formControlName="value" maxlength="4000" autocomplete="off" />
                  <button type="button" class="btn join-item" (click)="secretForm.controls.value.setValue(secret())" [title]="'Clients.Secrets.GenerateValue' | translate">
                    <svg lucideDices class="size-4"></svg>
                  </button>
                </div>
                <p class="label whitespace-normal">{{ 'Clients.Add.SecretValueHint' | translate }}</p>
              </fieldset>
              <div class="grid gap-3 sm:grid-cols-2">
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">{{ 'Clients.Secrets.ExpiresOn' | translate }}</legend>
                  <input type="date" class="input w-full" formControlName="expiration" />
                </fieldset>
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">{{ 'Common.Description' | translate }}</legend>
                  <input class="input w-full" formControlName="description" maxlength="2000" />
                </fieldset>
              </div>
              <button type="button" class="btn btn-sm" [disabled]="secretForm.invalid" (click)="addSharedSecret()">{{ 'Clients.Add.AddSecret' | translate }}</button>
            </div>
          } @else {
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ 'Clients.Secrets.PublicKey' | translate }} *</legend>
              <input #certificate type="file" class="file-input w-full" accept=".cer,.crt,.pem" (change)="addCertificate(certificate)" />
              <p class="label whitespace-normal">{{ 'Clients.Secrets.PublicKeyHint' | translate }}</p>
            </fieldset>
            @if (readingCertificate()) { <p class="flex items-center gap-2 text-sm"><span class="loading loading-spinner loading-xs"></span>{{ 'Clients.Add.ReadingCertificate' | translate }}</p> }
          }
        </div>
        <div>
          <h3 class="mb-2 text-sm font-semibold">{{ 'Clients.Add.SecretsToCreate' | translate }} <span class="badge badge-sm badge-ghost tabular">{{ secrets().length }}</span></h3>
          <ul class="divide-y divide-base-200 rounded-box border border-base-300">
            @for (item of secrets(); track $index) {
              <li class="flex items-start gap-3 px-4 py-3">
                <span class="mt-0.5 grid size-8 shrink-0 place-items-center rounded-field bg-base-200 text-base-content/70">
                  @if (item.type === 'SharedSecret') { <svg lucideKeyRound class="size-4"></svg> } @else { <svg lucideFileBadge class="size-4"></svg> }
                </span>
                <div class="min-w-0 flex-1 text-sm">
                  <p class="font-medium">{{ item.type === 'SharedSecret' ? ('Clients.Secrets.SharedSecret' | translate) : (item.file?.name ?? ('Clients.Secrets.Certificate' | translate)) }}</p>
                  <p class="truncate font-mono text-xs text-base-content/60">{{ item.value }}</p>
                  <p class="text-xs text-base-content/60">
                    {{ item.expiration ? ('Clients.Secrets.ExpiresOnDate' | translate: { date: (item.expiration | date: 'mediumDate') }) : ('Clients.Secrets.NeverExpires' | translate) }}{{ item.description ? ' · ' + item.description : '' }}
                  </p>
                </div>
                <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Clients.Add.RemoveSecret' | translate" (click)="removeSecret($index)">
                  <svg lucideTrash class="size-4"></svg>
                </button>
              </li>
            } @empty {
              <li class="px-4 py-8 text-center text-sm text-base-content/60">{{ 'Clients.Add.NoSecrets' | translate }}</li>
            }
          </ul>
        </div>
      </div>
    </ng-template>

    <ng-template #summaryStep>
      <dl class="grid max-w-3xl gap-x-8 gap-y-4 text-sm sm:grid-cols-[12rem_1fr]">
        <dt class="text-base-content/60">{{ 'Clients.Add.ApplicationType' | translate }}</dt><dd class="font-medium">{{ type()?.name ?? '' | translate }} <span class="text-base-content/60">· {{ type()?.flow ?? '' | translate }}</span></dd>
        <dt class="text-base-content/60">{{ 'Clients.Fields.ClientId' | translate }}</dt><dd><code class="code-chip">{{ form.value.clientId }}</code></dd>
        <dt class="text-base-content/60">{{ 'Common.DisplayName' | translate }}</dt><dd class="font-medium">{{ form.value.clientName }}</dd>
        @if (form.value.description) { <dt class="text-base-content/60">{{ 'Common.Description' | translate }}</dt><dd>{{ form.value.description }}</dd> }
        @if (hasStep('details')) {
          <dt class="text-base-content/60">{{ 'Clients.Fields.RequireConsent' | translate }}</dt><dd>{{ (form.value.requireConsent ? 'Common.Yes' : 'Common.No') | translate }}</dd>
          @if (form.value.clientUrl) { <dt class="text-base-content/60">{{ 'Clients.Fields.ClientUrl' | translate }}</dt><dd class="break-all">{{ form.value.clientUrl }}</dd> }
          @if (form.value.logoUrl) { <dt class="text-base-content/60">{{ 'Clients.Fields.LogoUrl' | translate }}</dt><dd class="break-all">{{ form.value.logoUrl }}</dd> }
        }
        @if (hasStep('urls')) {
          <dt class="text-base-content/60">{{ 'Clients.Fields.CallbackUrl' | translate }}</dt><dd class="font-mono text-xs break-all">{{ form.value.callbackUrl }}</dd>
          @if (form.value.postLogoutUrl) { <dt class="text-base-content/60">{{ 'Clients.Fields.PostLogoutUrl' | translate }}</dt><dd class="font-mono text-xs break-all">{{ form.value.postLogoutUrl }}</dd> }
        }
        @if (hasStep('secrets')) {
          <dt class="text-base-content/60">{{ 'Clients.Tabs.Secrets' | translate }}</dt><dd>{{ secrets().length || ('Common.None' | translate) }}</dd>
        }
        @if (hasStep('identity')) {
          <dt class="text-base-content/60">{{ 'Clients.Resources.IdentityResources' | translate }}</dt>
          <dd class="flex flex-wrap gap-1.5">@for (name of identity(); track name) { <span class="badge badge-sm badge-primary badge-soft">{{ name }}</span> }</dd>
        }
        <dt class="text-base-content/60">{{ 'Clients.Resources.ApiScopes' | translate }}</dt>
        <dd class="flex flex-wrap gap-1.5">
          @for (name of api(); track name) { <span class="badge badge-sm badge-secondary badge-soft">{{ name }}</span> } @empty { {{ 'Common.None' | translate }} }
        </dd>
      </dl>
    </ng-template>
  `
})
export class ClientAddComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  private readonly _fb = inject(NonNullableFormBuilder);

  protected readonly types = CLIENT_TYPES;
  protected readonly selected = signal<ClientTypeOption | null>(null);
  protected readonly type = signal<ClientTypeOption | null>(null);
  protected readonly index = signal(0);
  protected readonly stepTouched = signal(false);
  protected readonly saving = signal(false);
  protected readonly steps = computed<WizardStep[]>(() => this.type()?.steps ?? []);
  protected readonly step = computed(() => this.steps()[this.index()]);
  protected readonly stepTitles = computed(() => this.steps().map(step => this._translate.instant(STEP_TITLES[step]) as string));

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
        this._toast.success(this._translate.instant('Clients.Add.CreatedTitle'),
          this._translate.instant('Clients.Add.CreatedMessage', { name: created.clientName || created.clientId }));
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
