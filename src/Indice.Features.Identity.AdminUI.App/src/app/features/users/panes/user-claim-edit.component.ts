import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder, Validators } from '@angular/forms';
import { forkJoin, map } from 'rxjs';
import { ClaimTypeInfo, CreateClaimRequest, IdentityApiService, UpdateUserClaimRequest } from 'src/app/core/services/identity-api.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClaimValueInputComponent } from '../claim-value-input.component';
import { USER_REFRESH_SCOPE } from '../user-store';
import { claimValueValidators, isAssignableClaimType } from '../user-utils';

/** Side pane that adds a claim to the user in `userId`, or edits the value of the claim in `claimId`. */
@Component({
  selector: 'app-user-claim-edit',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, ClaimValueInputComponent],
  template: `
    <app-side-pane-layout [title]="claimId() ? 'Edit claim' : 'Add claim'"
      [subtitle]="claimId() ? 'Change the value of this claim.' : 'Store an additional detail about the user.'">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="user-claim-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Claim *</legend>
            @if (claimId()) {
              <p class="font-medium">{{ selectedType()?.displayName || form.controls.type.value }}</p>
              <p class="font-mono text-xs text-base-content/60">{{ form.controls.type.value }}</p>
            } @else {
              <select class="select w-full" formControlName="type" (change)="typeChanged()">
                <option value="" disabled>Select a claim…</option>
                @for (type of assignable(); track type.id) {
                  <option [value]="type.name">{{ type.displayName || type.name }}</option>
                }
              </select>
            }
            @if (selectedType()?.description) { <p class="label whitespace-normal">{{ selectedType()?.description }}</p> }
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Value *</legend>
            <app-claim-value-input formControlName="value" [valueType]="selectedType()?.valueType"
              [invalid]="form.controls.value.invalid && form.controls.value.touched" />
            @if (form.controls.value.touched && form.controls.value.hasError('pattern')) {
              <p class="label text-error">The value does not have the expected format.</p>
            } @else if (form.controls.value.touched && form.controls.value.hasError('required')) {
              <p class="label text-error">A value is required.</p>
            }
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">Cancel</button>
        <button type="submit" form="user-claim-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ claimId() ? 'Save' : 'Add claim' }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class UserClaimEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `user-claim/:userId(/:claimId)` pane routes. */
  public readonly userId = input.required<string>();
  public readonly claimId = input<string>();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly types = signal<ClaimTypeInfo[]>([]);
  protected readonly assignable = computed(() => this.types().filter(isAssignableClaimType));
  protected readonly selectedType = signal<ClaimTypeInfo | undefined>(undefined);
  protected readonly form = inject(NonNullableFormBuilder).group({
    type: ['', Validators.required],
    value: ['', Validators.required]
  });

  public ngOnInit(): void {
    const claimId = this.claimId();
    forkJoin([
      this._api.getClaimTypes(1, 2147483647, 'name+', undefined).pipe(map(result => result.items ?? [])),
      this._api.getUser(this.userId())
    ]).subscribe({
      next: ([types, user]) => {
        this.types.set(types);
        if (claimId) {
          const claim = user.claims?.find(c => String(c.id) === claimId);
          if (!claim) {
            this.pane.close();
            return;
          }
          this.form.setValue({ type: claim.type ?? '', value: claim.value ?? '' });
          this.applyType();
        }
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected typeChanged(): void {
    this.applyType();
    this.form.controls.value.setValue('');
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const { type, value } = this.form.getRawValue();
    const claimId = this.claimId();
    const request$ = claimId
      ? this._api.updateUserClaim(this.userId(), +claimId, new UpdateUserClaimRequest({ claimValue: value }))
      : this._api.addUserClaim(this.userId(), new CreateClaimRequest({ type, value }));
    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this._toast.success(claimId ? 'Claim updated' : 'Claim added', `Claim '${type}' was saved.`);
        this._refresh.notify(USER_REFRESH_SCOPE);
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  private applyType(): void {
    const type = this.types().find(t => t.name === this.form.controls.type.value);
    this.selectedType.set(type);
    this.form.controls.value.setValidators(claimValueValidators(type));
    this.form.controls.value.updateValueAndValidity();
  }
}
