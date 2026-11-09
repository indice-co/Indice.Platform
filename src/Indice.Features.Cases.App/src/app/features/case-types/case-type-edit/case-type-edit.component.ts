import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { toObservable, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { filter, finalize, switchMap } from 'rxjs';
import { LucideSave } from '@lucide/angular';
import { CasesApiService, CaseType } from 'src/app/core/services/cases-api.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { TABS } from 'src/app/ui/tabs/tabs';
import { CheckpointTypesComponent } from '../../checkpoint-types/checkpoint-types.component';
import { CaseTypeConfigurationComponent } from '../case-type-form/case-type-configuration.component';
import { CaseTypeDefinitionComponent } from '../case-type-form/case-type-definition.component';
import { CaseTypeFormState } from '../case-type-form/case-type-form.state';
import { CaseTypeGeneralComponent } from '../case-type-form/case-type-general.component';
import { CaseTypeUpdateService } from '../case-type-update.service';

const TAB_IDS = ['general', 'definition', 'configuration', 'checkpoints'] as const;
const CHECKPOINTS_TAB = TAB_IDS.indexOf('checkpoints');

/** Edit a case type: the parts of the create wizard as tabs, with the case type's checkpoint types as the last one. */
@Component({
  selector: 'app-case-type-edit',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, TABS, CaseTypeGeneralComponent, CaseTypeDefinitionComponent,
    CaseTypeConfigurationComponent, CheckpointTypesComponent, LucideSave
  ],
  template: `
    <app-page-header [title]="'caseTypes.edit.title' | translate" [subtitle]="caseType()?.title">
      @if (caseType()?.code) {
        <p class="mt-2"><span class="badge badge-ghost font-mono">{{ caseType()?.code }}</span></p>
      }
    </app-page-header>
    @if (caseType()) {
      <app-tab-group [(selectedIndex)]="tab">
        <app-tab id="general" [label]="'caseTypes.steps.general' | translate">
          <ng-template appTabLabel>
            {{ 'caseTypes.steps.general' | translate }}
            @if (!state.generalValid()) { <span class="status status-error" [attr.aria-label]="'common.hasErrors' | translate"></span> }
          </ng-template>
          <app-case-type-general class="mt-6" [state]="state" />
        </app-tab>
        <app-tab id="definition" [label]="'caseTypes.steps.definition' | translate">
          <ng-template appTabLabel>
            {{ 'caseTypes.steps.definition' | translate }}
            @if (!state.definitionValid()) { <span class="status status-error" [attr.aria-label]="'common.hasErrors' | translate"></span> }
          </ng-template>
          <app-case-type-definition class="mt-6" [state]="state" />
        </app-tab>
        <app-tab id="configuration" [label]="'caseTypes.steps.configuration' | translate">
          <ng-template appTabLabel>
            {{ 'caseTypes.steps.configuration' | translate }}
            @if (!state.configurationValid()) { <span class="status status-error" [attr.aria-label]="'common.hasErrors' | translate"></span> }
          </ng-template>
          <app-case-type-configuration class="mt-6" [state]="state" />
        </app-tab>
        <app-tab id="checkpoints" [label]="'checkpointTypes.title' | translate">
          <ng-template appTabContent>
            <app-checkpoint-types class="mt-6 block" [scopeCaseTypeId]="caseTypeId()" [embedded]="true" [returnUrl]="returnUrl()" />
          </ng-template>
        </app-tab>
      </app-tab-group>

      @if (tab() !== CHECKPOINTS_TAB) {
        <div class="sticky bottom-0 z-10 -mx-4 mt-6 flex items-center justify-end gap-2 border-t border-base-300 bg-base-200/90 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8">
          @if (!state.valid()) {
            <p class="mr-auto text-sm text-error">{{ 'caseTypes.edit.fixTabs' | translate }}</p>
          }
          <a routerLink="/case-types" class="btn btn-ghost">{{ 'common.cancel' | translate }}</a>
          <button type="button" class="btn btn-primary" [disabled]="!state.valid() || busy()" (click)="save()">
            @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'common.save' | translate }}
          </button>
        </div>
      }
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
  private readonly _router = inject(Router);

  /** Bound from the `:caseTypeId` route param. */
  public readonly caseTypeId = input.required<string>();
  /** Bound from the `tab` query param: the id of the tab to open with (`checkpoints` when coming back from a checkpoint type). */
  public readonly initialTab = input<string | undefined>(undefined, { alias: 'tab' });

  protected readonly CHECKPOINTS_TAB = CHECKPOINTS_TAB;
  protected readonly state = new CaseTypeFormState();
  protected readonly caseType = signal<CaseType | undefined>(undefined);
  protected readonly busy = signal(false);
  protected readonly tab = linkedSignal(() => Math.max(0, TAB_IDS.indexOf(this.initialTab() as typeof TAB_IDS[number])));
  /** Where the checkpoint type pages return to: the checkpoints tab. */
  protected readonly returnUrl = computed(() =>
    this._router.serializeUrl(this._router.createUrlTree(['/case-types', this.caseTypeId(), 'edit'], { queryParams: { tab: 'checkpoints' } })));

  constructor() {
    toObservable(this.caseTypeId)
      .pipe(
        filter(id => !!id),
        switchMap(id => this._api.getCaseTypeById(id)),
        takeUntilDestroyed()
      )
      .subscribe(caseType => {
        this.state.load(caseType);
        this.caseType.set(caseType);
      });
  }

  protected save(): void {
    if (!this.state.valid() || this.busy()) {
      return;
    }
    this.busy.set(true);
    this._caseTypes.onEditSubmit(this.caseTypeId(), this.state.toRequest()).pipe(finalize(() => this.busy.set(false))).subscribe();
  }
}
