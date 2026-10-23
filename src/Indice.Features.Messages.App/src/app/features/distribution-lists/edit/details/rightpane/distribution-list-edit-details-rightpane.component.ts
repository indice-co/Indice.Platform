import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { MessagesApiClient, UpdateDistributionListRequest } from 'src/app/core/services/messages-api.service';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane form that edits the name and alias of the distribution list in the `distributionListId` matrix param. */
@Component({
  selector: 'app-distribution-list-details-edit-rightpane',
  imports: [ReactiveFormsModule, TranslatePipe, SidePaneLayoutComponent],
  template: `
    <app-side-pane-layout [title]="'DistributionLists.EditList' | translate">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="distribution-list-edit-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="distribution-list-edit-name">{{ 'DistributionLists.ListTitleLabel' | translate }}</label>
            <input id="distribution-list-edit-name" class="input w-full" formControlName="name" maxlength="128" [placeholder]="'DistributionLists.ListNamePlaceholder' | translate"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">{{ 'DistributionLists.ListNameRequired' | translate }}</p>
            } @else if (form.controls.name.hasError('maxlength')) {
              <p class="label text-error">{{ 'DistributionLists.ListNameMaxLength' | translate }}</p>
            } @else {
              <p class="label whitespace-normal">{{ 'DistributionLists.ListInfo' | translate }}</p>
            }
          </fieldset>
          <fieldset class="fieldset">
            <label class="fieldset-legend" for="distribution-list-edit-alias">{{ 'DistributionLists.AliasLabel' | translate }}</label>
            <input id="distribution-list-edit-alias" class="input w-full font-mono" formControlName="alias" maxlength="64" [placeholder]="'DistributionLists.AliasPlaceholder' | translate"
              [class.input-error]="form.controls.alias.invalid && form.controls.alias.touched" />
            @if (form.controls.alias.hasError('maxlength')) {
              <p class="label text-error">{{ 'DistributionLists.AliasMaxLength' | translate }}</p>
            }
          </fieldset>
        </form>
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'DistributionLists.CancelLabel' | translate }}</button>
        <button type="submit" form="distribution-list-edit-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'DistributionLists.Save' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class DistributionListDetailsEditRightpaneComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);

  /** Matrix param of the `edit-distribution-list` pane route. */
  public readonly distributionListId = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    alias: ['', [Validators.required, Validators.maxLength(64)]]
  });

  public ngOnInit(): void {
    this._api.getDistributionListById(this.distributionListId()).subscribe({
      next: list => {
        this.form.setValue({ name: list.name ?? '', alias: list.alias ?? '' });
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.saving.set(true);
    this._api.updateDistributionList(this.distributionListId(), new UpdateDistributionListRequest(value)).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'DistributionLists.UpdateSuccessTitle', 'DistributionLists.UpdateSuccessMessage', undefined, { name: value.name });
        this._refresh.notify('distribution-list');
        this._refresh.notify('distribution-lists');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
