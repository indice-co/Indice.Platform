import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { LucideArrowLeft, LucideArrowRight, LucideBoxes, LucideCheck, LucideIdCard, LucideKeyRound } from '@lucide/angular';
import { CreateApiScopeRequest, CreateResourceRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { StepperComponent } from 'src/app/ui/stepper/stepper.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimsAssignmentComponent } from '../shared/claims-assignment.component';

type ResourceType = 'identity' | 'api' | 'scope';

const TYPES: { value: ResourceType; label: string; hint: string }[] = [
  { value: 'identity', label: 'Identity resource', hint: 'User claims about the signed-in user, such as profile or email.' },
  { value: 'api', label: 'API resource', hint: 'An API protected by the identity server, with its own scopes and secrets.' },
  { value: 'scope', label: 'API scope', hint: 'A permission clients request to call one or more APIs.' }
];

/** Wizard that creates an identity resource, API resource or API scope. `history.state.origin` preselects the type. */
@Component({
  selector: 'app-resource-add',
  imports: [
    ReactiveFormsModule, RouterLink, PageHeaderComponent, StepperComponent, ClaimsAssignmentComponent,
    LucideArrowLeft, LucideArrowRight, LucideBoxes, LucideCheck, LucideIdCard, LucideKeyRound
  ],
  template: `
    <app-page-header title="New resource" subtitle="Describe what clients can request: user identity data, an API, or a permission on an API.">
      <a leading [routerLink]="backLink()" class="mb-2 inline-flex items-center gap-1 text-sm text-base-content/60 hover:text-base-content">
        <svg lucideArrowLeft class="size-4"></svg>Back
      </a>
    </app-page-header>

    <div class="panel max-w-4xl">
      <app-stepper class="border-b border-base-300 px-6 py-4" [steps]="steps" [current]="step()" (select)="step.set($event)" />

      <form class="p-6" [formGroup]="form" (ngSubmit)="next()">
        @switch (step()) {
          @case (0) {
            <div class="space-y-5">
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Type</legend>
                <div class="grid gap-3 sm:grid-cols-3">
                  @for (type of types; track type.value) {
                    <label class="flex cursor-pointer flex-col gap-2 rounded-box border border-base-300 p-4 has-checked:border-primary has-checked:bg-primary/5">
                      <span class="flex items-center gap-2">
                        <input type="radio" class="radio radio-sm radio-primary" formControlName="type" [value]="type.value" />
                        @switch (type.value) {
                          @case ('identity') { <svg lucideIdCard class="size-4 text-primary"></svg> }
                          @case ('api') { <svg lucideBoxes class="size-4 text-primary"></svg> }
                          @default { <svg lucideKeyRound class="size-4 text-primary"></svg> }
                        }
                        <span class="font-semibold">{{ type.label }}</span>
                      </span>
                      <span class="text-xs text-base-content/65">{{ type.hint }}</span>
                    </label>
                  }
                </div>
              </fieldset>
              <div class="grid gap-4 sm:grid-cols-2">
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Name *</legend>
                  <input class="input w-full font-mono" formControlName="name" maxlength="200" [placeholder]="namePlaceholder()"
                    [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
                  @if (form.controls.name.touched && form.controls.name.hasError('required')) {
                    <p class="label text-error">A name is required.</p>
                  } @else {
                    <p class="label">Unique, and cannot change later.</p>
                  }
                </fieldset>
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">Display name</legend>
                  <input class="input w-full" formControlName="displayName" maxlength="200" />
                </fieldset>
              </div>
              <fieldset class="fieldset">
                <legend class="fieldset-legend">Description</legend>
                <textarea class="textarea w-full" rows="3" formControlName="description" maxlength="1000"></textarea>
              </fieldset>
            </div>
          }
          @case (1) {
            <p class="mb-4 max-w-prose text-sm text-base-content/70">{{ claimsHint() }}</p>
            <app-claims-assignment [assigned]="claims()" (added)="addClaim($event)" (removed)="removeClaim($event)" />
          }
          @default {
            <dl class="grid gap-x-6 gap-y-4 sm:grid-cols-[10rem_1fr]">
              <dt class="text-sm text-base-content/60">Type</dt>
              <dd class="font-medium">{{ typeLabel() }}</dd>
              <dt class="text-sm text-base-content/60">Name</dt>
              <dd><span class="code-chip">{{ form.controls.name.value }}</span></dd>
              <dt class="text-sm text-base-content/60">Display name</dt>
              <dd>{{ form.controls.displayName.value || '—' }}</dd>
              <dt class="text-sm text-base-content/60">Description</dt>
              <dd class="whitespace-pre-line">{{ form.controls.description.value || '—' }}</dd>
              <dt class="text-sm text-base-content/60">User claims</dt>
              <dd class="flex flex-wrap gap-1.5">
                @for (claim of claims(); track claim) { <span class="badge badge-sm badge-ghost font-mono">{{ claim }}</span> }
                @empty { <span class="text-base-content/50">None</span> }
              </dd>
            </dl>
          }
        }

        <div class="mt-8 flex items-center justify-between gap-2 border-t border-base-300 pt-4">
          <button type="button" class="btn btn-ghost" [disabled]="step() === 0" (click)="step.set(step() - 1)">
            <svg lucideArrowLeft class="size-4"></svg>Back
          </button>
          @if (step() < steps.length - 1) {
            <button type="submit" class="btn btn-primary">Next<svg lucideArrowRight class="size-4"></svg></button>
          } @else {
            <button type="submit" class="btn btn-primary" [disabled]="saving()">
              @if (saving()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideCheck class="size-4"></svg> }
              Create {{ typeLabel().toLowerCase() }}
            </button>
          }
        </div>
      </form>
    </div>
  `
})
export class ResourceAddComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _router = inject(Router);
  private readonly _toast = inject(ToastService);

  protected readonly types = TYPES;
  protected readonly steps = ['Basics', 'User claims', 'Review'];
  protected readonly step = signal(0);
  protected readonly saving = signal(false);
  protected readonly claims = signal<string[]>([]);
  protected readonly form = inject(NonNullableFormBuilder).group({
    type: [toType(history.state?.origin), Validators.required],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    displayName: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(1000)]
  });

  private readonly _type = signal<ResourceType>(this.form.controls.type.value);
  protected readonly typeLabel = computed(() => TYPES.find(t => t.value === this._type())!.label);
  protected readonly namePlaceholder = computed(() => ({ identity: 'e.g. profile', api: 'e.g. orders-api', scope: 'e.g. orders:read' })[this._type()]);
  protected readonly claimsHint = computed(() => ({
    identity: 'Pick the user claims included in the identity token when a client requests this resource.',
    api: 'Pick the user claims included in every access token issued for this API.',
    scope: 'Pick the user claims included in access tokens when a client is granted this scope.'
  })[this._type()]);
  protected readonly backLink = computed(() => ({ identity: '/resources/identity', api: '/resources/api', scope: '/resources/scopes' })[this._type()]);

  constructor() {
    this.form.controls.type.valueChanges.subscribe(type => this._type.set(type));
  }

  protected addClaim(claim: string): void {
    this.claims.update(claims => [...claims, claim]);
  }

  protected removeClaim(claim: string): void {
    this.claims.update(claims => claims.filter(name => name !== claim));
  }

  protected next(): void {
    if (this.step() === 0 && this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.step() < this.steps.length - 1) {
      this.step.update(step => step + 1);
      return;
    }
    this.create();
  }

  private create(): void {
    const value = this.form.getRawValue();
    const base = {
      name: value.name.trim(),
      displayName: value.displayName || undefined,
      description: value.description || undefined,
      userClaims: this.claims()
    };
    let request$: Observable<{ id?: number; name?: string }>;
    switch (value.type) {
      case 'identity':
        request$ = this._api.createIdentityResource(new CreateResourceRequest(base));
        break;
      case 'api':
        request$ = this._api.createApiResource(new CreateResourceRequest(base));
        break;
      default:
        request$ = this._api.createApiScope(new CreateApiScopeRequest({
          ...base, required: false, emphasize: false, showInDiscoveryDocument: true, translations: {}
        }));
    }
    this.saving.set(true);
    request$.subscribe({
      next: created => {
        this._toast.success(`${this.typeLabel()} created`, `${this.typeLabel()} '${created.name}' was created.`);
        const target = value.type === 'scope' ? ['/resources/scopes', created.name!]
          : value.type === 'api' ? ['/resources/api', String(created.id)] : ['/resources/identity', String(created.id)];
        this._router.navigate(target);
      },
      error: () => this.saving.set(false)
    });
  }
}

function toType(origin: unknown): ResourceType {
  return origin === 'api' || origin === 'scope' ? origin : 'identity';
}
