import { Component, inject, input, signal } from '@angular/core';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, finalize, switchMap } from 'rxjs';
import { CasesApiService, CaseType, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { CheckpointTypesComponent } from '../../checkpoint-types/checkpoint-types.component';
import { CaseTypeFormComponent } from '../case-type-form/case-type-form.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

@Component({
  selector: 'app-case-type-edit',
  imports: [TranslatePipe, PageHeaderComponent, CaseTypeFormComponent, CheckpointTypesComponent],
  template: `
    <app-page-header [title]="'caseTypes.edit.title' | translate" [subtitle]="caseType()?.title">
      @if (caseType()?.code) {
        <p class="mt-2"><span class="badge badge-ghost font-mono">{{ caseType()?.code }}</span></p>
      }
    </app-page-header>
    @if (caseType(); as data) {
      <app-case-type-form [data]="data" [busy]="busy()" (submitted)="onSubmit($event)" />
      <section class="mt-8">
        <h2 class="mb-3 text-lg font-semibold">{{ 'checkpointTypes.title' | translate }}</h2>
        <app-checkpoint-types [scopeCaseTypeId]="caseTypeId()" [embedded]="true" />
      </section>
    } @else {
      <div class="panel space-y-4 p-6">
        <div class="skeleton h-5 w-48"></div>
        <div class="skeleton h-10 w-full"></div>
        <div class="skeleton h-10 w-full"></div>
        <div class="skeleton h-40 w-full"></div>
      </div>
    }
  `
})
export class CaseTypeEditComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypes = inject(CaseTypeUpdateService);

  /** Bound from the `:caseTypeId` route param. */
  public readonly caseTypeId = input.required<string>();
  protected readonly caseType = signal<CaseType | undefined>(undefined);
  protected readonly busy = signal(false);

  constructor() {
    toObservable(this.caseTypeId)
      .pipe(
        filter(id => !!id),
        switchMap(id => this._api.getCaseTypeById(id)),
        takeUntilDestroyed()
      )
      .subscribe(caseType => this.caseType.set(caseType));
  }

  protected onSubmit(request: ICaseTypeRequest): void {
    this.busy.set(true);
    this._caseTypes.onEditSubmit(this.caseTypeId(), request).pipe(finalize(() => this.busy.set(false))).subscribe();
  }
}
