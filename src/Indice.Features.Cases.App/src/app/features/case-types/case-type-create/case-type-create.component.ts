import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, finalize, switchMap } from 'rxjs';
import { LucideArrowLeft, LucideArrowRight, LucideCheck, LucideSave } from '@lucide/angular';
import { CasesApiService } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { STEPPER } from 'src/app/ui/stepper/stepper';
import { CheckpointTypesComponent } from '../../checkpoint-types/checkpoint-types.component';
import { CaseTypeConfigurationComponent } from '../case-type-form/case-type-configuration.component';
import { CaseTypeDefinitionComponent } from '../case-type-form/case-type-definition.component';
import { CaseTypeFormState } from '../case-type-form/case-type-form.state';
import { CaseTypeGeneralComponent } from '../case-type-form/case-type-general.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

/** Index of the checkpoint types step, the one after the case type is saved. */
const CHECKPOINTS_STEP = 3;

/**
 * New case type wizard: general → schema, layout & translations → configuration (saves the case type) → checkpoint types.
 * Once saved, the `caseTypeId` query param holds the new case type, so leaving to add a checkpoint type comes back to the last step.
 */
@Component({
  selector: 'app-case-type-create',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, STEPPER, CaseTypeGeneralComponent, CaseTypeDefinitionComponent,
    CaseTypeConfigurationComponent, CheckpointTypesComponent, LucideArrowLeft, LucideArrowRight, LucideCheck, LucideSave
  ],
  template: `
    <app-page-header [title]="'caseTypes.create.title' | translate" [subtitle]="createdTitle()" />

    <app-stepper #stepper [(selectedIndex)]="step">
      <app-step [label]="'caseTypes.steps.general' | translate" [hint]="'caseTypes.steps.generalHint' | translate"
        [completed]="saved() || state.generalValid()" [invalid]="!saved() && !state.generalValid()" [editable]="!saved()">
        <app-case-type-general [state]="state" />
      </app-step>
      <app-step [label]="'caseTypes.steps.definition' | translate" [hint]="'caseTypes.steps.definitionHint' | translate"
        [completed]="saved() || state.definitionValid()" [invalid]="!saved() && !state.definitionValid()" [editable]="!saved()">
        <app-case-type-definition [state]="state" />
      </app-step>
      <app-step [label]="'caseTypes.steps.configuration' | translate" [hint]="'caseTypes.steps.configurationHint' | translate"
        [completed]="saved()" [invalid]="!saved() && !state.configurationValid()" [editable]="!saved()">
        <app-case-type-configuration [state]="state" />
      </app-step>
      <app-step [label]="'checkpointTypes.title' | translate" [hint]="'caseTypes.steps.checkpointsHint' | translate">
        <ng-template appStepContent>
          <p class="mb-4 max-w-prose text-sm text-base-content/70">{{ 'caseTypes.steps.checkpointsIntro' | translate }}</p>
          <app-checkpoint-types [scopeCaseTypeId]="createdId()" [embedded]="true" [returnUrl]="returnUrl()" />
        </ng-template>
      </app-step>
    </app-stepper>

    <div class="sticky bottom-0 z-10 -mx-4 mt-6 flex flex-wrap items-center justify-end gap-2 border-t border-base-300 bg-base-200/90 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8">
      @if (blocker(); as blocker) {
        <p class="mr-auto text-sm text-error">{{ blocker | translate }}</p>
      }
      @if (saved()) {
        <a routerLink="/case-types" class="btn btn-primary"><svg lucideCheck class="size-4"></svg>{{ 'common.done' | translate }}</a>
      } @else {
        <a routerLink="/case-types" class="btn btn-ghost">{{ 'common.cancel' | translate }}</a>
        @if (stepper.canGoBack()) {
          <button type="button" class="btn" (click)="stepper.previous()"><svg lucideArrowLeft class="size-4"></svg>{{ 'common.return' | translate }}</button>
        }
        @if (step() < CHECKPOINTS_STEP - 1) {
          <button type="button" class="btn btn-primary" [disabled]="!stepper.canGoNext()" (click)="stepper.next()">
            {{ 'common.next' | translate }}<svg lucideArrowRight class="size-4"></svg>
          </button>
        } @else {
          <button type="button" class="btn btn-primary" [disabled]="!state.valid() || busy()" (click)="save()">
            @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'caseTypes.steps.saveAndContinue' | translate }}
          </button>
        }
      }
    </div>
  `
})
export class CaseTypeCreateComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypes = inject(CaseTypeUpdateService);
  private readonly _router = inject(Router);
  private readonly _route = inject(ActivatedRoute);
  /** Title of the created case type, loaded when coming back to the wizard (the form state is gone by then). */
  private readonly _loadedTitle = signal<string | undefined>(undefined);

  /** Bound from the `caseTypeId` query param: the case type this wizard already created. */
  public readonly caseTypeId = input<string>();

  protected readonly CHECKPOINTS_STEP = CHECKPOINTS_STEP;
  protected readonly state = new CaseTypeFormState();
  protected readonly busy = signal(false);
  protected readonly createdId = linkedSignal(() => this.caseTypeId());
  protected readonly step = linkedSignal<number>(() => this.caseTypeId() ? CHECKPOINTS_STEP : 0);
  protected readonly saved = computed(() => !!this.createdId());
  protected readonly createdTitle = computed(() => this.saved() ? this.state.general().title || this._loadedTitle() : undefined);
  /** Where the checkpoint type pages return to: this step. */
  protected readonly returnUrl = computed(() =>
    this._router.serializeUrl(this._router.createUrlTree(['/case-types/create'], { queryParams: { caseTypeId: this.createdId() } })));

  /** Why the user cannot move on from the current step, if anything is in the way. */
  protected readonly blocker = computed(() => {
    if (this.saved()) {
      return undefined;
    }
    switch (this.step()) {
      case 0: return this.state.generalValid() ? undefined : 'caseTypes.steps.fillRequired';
      case 1: return this.state.definitionValid() ? undefined
        : !this.state.schemaValid() ? 'caseTypes.steps.schemaRequired'
        : !this.state.layoutValid() ? 'caseTypes.steps.fixJson'
        : 'caseTypes.translations.fixTitles';
      default: return this.state.configurationValid() ? undefined : 'caseTypes.steps.fixJson';
    }
  });

  constructor() {
    toObservable(this.caseTypeId)
      .pipe(
        filter(id => !!id && !this.state.general().title),
        switchMap(id => this._api.getCaseTypeById(id!)),
        takeUntilDestroyed()
      )
      .subscribe(caseType => this._loadedTitle.set(caseType.title));
  }

  protected save(): void {
    if (!this.state.valid() || this.busy()) {
      return;
    }
    this.busy.set(true);
    this._caseTypes.onCreateSubmit(this.state.toRequest()).pipe(finalize(() => this.busy.set(false))).subscribe(id => {
      if (!id) {
        this._router.navigate(['/case-types']);
        return;
      }
      this.createdId.set(id);
      this.step.set(CHECKPOINTS_STEP);
      this._router.navigate([], { relativeTo: this._route, queryParams: { caseTypeId: id }, replaceUrl: true });
    });
  }
}
