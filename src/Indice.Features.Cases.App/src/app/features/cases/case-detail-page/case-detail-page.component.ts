import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, Subscription, of } from 'rxjs';
import { distinctUntilChanged, filter, map, switchMap } from 'rxjs/operators';
import {
  LucideArrowLeft, LucideCalendar, LucideFolderX, LucideInbox, LucideTrash, LucideUser, LucideUserCheck
} from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import {
  ActionRequest, Case, CaseActions, CasePartial, CaseStatus, CasesApiService, CustomCaseAction, TimelineEntry
} from 'src/app/core/services/cases-api.service';
import { ApprovalButtonsComponent } from 'src/app/shared/components/approval-buttons/approval-buttons.component';
import { CaseCustomActionComponent, CustomActionEvent } from 'src/app/shared/components/case-custom-action/case-custom-action.component';
import { CaseDetailInfoComponent, InfoEntry } from 'src/app/shared/components/case-detail-info/case-detail-info.component';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { CaseCommentComposerComponent } from './case-comment-composer/case-comment-composer.component';
import { CaseFormComponent } from './case-form/case-form.component';
import { CasePrintPdfComponent } from './case-print-pdf/case-print-pdf.component';
import { CaseTimelineComponent } from './case-timeline/case-timeline.component';
import { RelatedCasesComponent } from './related-cases/related-cases.component';

@Component({
  selector: 'app-case-detail-page',
  templateUrl: './case-detail-page.component.html',
  imports: [
    DatePipe, RouterLink, TranslatePipe, ApprovalButtonsComponent, CaseCommentComposerComponent, CaseCustomActionComponent, CaseDetailInfoComponent, CaseFormComponent,
    CasePrintPdfComponent, CaseTimelineComponent, RelatedCasesComponent, EmptyStateComponent,
    LucideArrowLeft, LucideCalendar, LucideFolderX, LucideInbox, LucideTrash, LucideUser, LucideUserCheck
  ]
})
export class CaseDetailPageComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _caseDetailsService = inject(CaseDetailsService);
  private readonly _router = inject(Router);
  private readonly _toaster = inject(TranslatedToasterService);
  private readonly _dialog = inject(DialogService);
  private readonly _destroyRef = inject(DestroyRef);
  private _caseId = '';
  private _modelSub?: Subscription;
  private _actionsSub?: Subscription;

  protected readonly model = signal<Case | undefined>(undefined);
  protected readonly caseActions = signal<CaseActions | undefined>(undefined);
  protected readonly timelineEntries = signal<TimelineEntry[] | undefined>(undefined);
  protected readonly relatedCases = signal<CasePartial[]>([]);
  protected readonly loadFailed = signal(false);

  protected readonly formValid = signal(false);
  protected readonly formUnSavedChanges = signal(false);
  /** Whether approving shows the "are you sure" warning dialog first. */
  protected readonly showWarningModal = signal(false);
  protected readonly assigning = signal(false);

  protected readonly caseTypeConfig = computed<any>(() => this.model()?.caseType?.config ?? {});
  protected readonly ready = computed(() => !!this.model() && !!this.caseActions());

  protected readonly statusClass = computed(() => {
    switch (this.model()?.checkpointType?.status) {
      case CaseStatus.Submitted: return 'stamp-submitted';
      case CaseStatus.InProgress: return 'stamp-inprogress';
      case CaseStatus.Completed: return 'stamp-completed';
      case CaseStatus.Deleted: return 'stamp-deleted';
      default: return 'stamp-neutral';
    }
  });

  /** PDF download is allowed on the checkpoints configured by the case type, or else on in-progress / completed cases. */
  protected readonly printEnabled = computed(() => {
    const model = this.model();
    const allowed: string[] | undefined = this.caseTypeConfig()?.checkpointsThatAllowDownload;
    if (allowed) {
      return allowed.includes(model?.checkpointType?.code ?? '');
    }
    const status = model?.checkpointType?.status;
    return status === CaseStatus.Completed || status === CaseStatus.InProgress;
  });

  /** Custom actions without a description or input fit in the header; the rest get their own card. */
  protected readonly headerActions = computed(() => (this.caseActions()?.customActions ?? []).filter(a => !a.description && !a.hasInput));
  protected readonly panelActions = computed(() => (this.caseActions()?.customActions ?? []).filter(a => !!a.description || !!a.hasInput));

  protected readonly caseFacts = computed<InfoEntry[]>(() => {
    const model = this.model();
    return [
      { key: 'caseDetails.caseId', value: model?.id, mono: true },
      { key: 'caseDetails.referenceNumber', value: model?.referenceNumber?.toString(), mono: true },
      { key: 'caseDetails.caseType', value: model?.caseType?.title },
      { key: 'caseDetails.checkpoint', value: model?.checkpointType?.title ?? model?.checkpointType?.code },
      { key: 'caseDetails.channel', value: model?.channel }
    ];
  });

  protected readonly customerInfo = computed<InfoEntry[]>(() => {
    const model = this.model();
    return [
      { key: 'caseDetails.customerName', value: model?.ownerName },
      { key: 'caseDetails.customerId', value: model?.ownerId, mono: true },
      { key: 'caseDetails.taxId', value: model?.ownerTin, mono: true },
      { key: 'caseDetails.groupId', value: model?.groupId, mono: true }
    ];
  });

  protected readonly userInfo = computed<InfoEntry[] | null>(() => {
    const metadata = this.model()?.metadata;
    if (!metadata?.['UserRole']) {
      return null;
    }
    return [
      { key: 'caseDetails.userSurname', value: metadata['UserSurname'] },
      { key: 'caseDetails.userFirstName', value: metadata['UserFirstName'] },
      { key: 'caseDetails.userRole', value: metadata['UserRole'] },
      { key: 'caseDetails.userCustomerId', value: metadata['UserCustomerId'], mono: true }
    ];
  });

  constructor() {
    inject(ActivatedRoute).paramMap
      .pipe(map(p => p.get('caseId') ?? ''), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(caseId => {
        this._caseId = caseId;
        this.model.set(undefined);
        this.caseActions.set(undefined);
        this.relatedCases.set([]);
        this.loadFailed.set(false);
        this.requestModel(true);
        this.getCaseActions();
        this.getTimeline();
      });
  }

  public updateData(): void {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
    this.showWarningModal.set(this.caseTypeConfig()?.boOptions?.showWarningModal !== false);
  }

  public onActionsChanged(): void {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
  }

  protected assignToMe(): void {
    this.assigning.set(true);
    this._api.assignCase(this._caseId).subscribe({
      next: () => {
        this.assigning.set(false);
        this.onActionsChanged();
      },
      error: () => this.assigning.set(false)
    });
  }

  protected discardDraft(): void {
    this._dialog.confirm({
      title: 'caseDetails.discardDraft.title',
      message: 'caseDetails.discardDraft.confirm',
      confirmLabel: 'caseDetails.discardDraft.title',
      tone: 'danger'
    })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteDraftCase(this._caseId)))
      .subscribe(() => {
        this._toaster.show(ToastType.Info, 'caseDetails.discardDraft.title', 'toasts.caseDiscarded.body');
        this._router.navigate(['/cases']);
      });
  }

  /** Registers whether the PDF was printed: approving after printing needs no extra warning. */
  protected onPdfButtonClicked(printed: boolean | undefined): void {
    if (printed === undefined) {
      return;
    }
    this.showWarningModal.set(this.caseTypeConfig()?.boOptions?.showWarningModal === false ? false : !printed);
  }

  /** Triggers a blocking workflow activity by its id. */
  protected onCustomActionTrigger(event: CustomActionEvent): void {
    this._api.triggerAction(this._caseId, new ActionRequest({ id: event.id, value: event.value })).subscribe({
      next: () => {
        if (event.redirectToList) {
          if (event.successMessage) {
            this._toaster.show(ToastType.Info, event.successMessage.title, event.successMessage.body);
          }
          this._router.navigate(['/cases']);
        } else {
          this.onActionsChanged();
        }
      },
      // reload the actions so the failed action's button is enabled again
      error: () => this.onActionsChanged()
    });
  }

  protected trackAction(action: CustomCaseAction): string {
    return action.id ?? action.label ?? '';
  }

  private requestModel(loadRelated = false): void {
    this._modelSub?.unsubscribe();
    this._modelSub = this._api.getCaseById(this._caseId)
      .pipe(
        // drafts must be prefilled with the contact's data
        switchMap(caseDetails => caseDetails.draft ? this.initializeData$(caseDetails) : of(caseDetails)),
        takeUntilDestroyed(this._destroyRef)
      )
      .subscribe({
        next: caseDetails => {
          // set before the form renders: the JSON Forms renderers read the case channel from it
          this._caseDetailsService.setCaseDetails(caseDetails);
          this.model.set(caseDetails);
          if (loadRelated) {
            this.getRelatedCases(caseDetails);
          }
        },
        error: () => this.loadFailed.set(true)
      });
  }

  private initializeData$(caseDetails: Case): Observable<Case> {
    return this._api.initializeCaseData(caseDetails.id!).pipe(
      map(contactData => {
        caseDetails.data = contactData;
        return caseDetails;
      })
    );
  }

  private getCaseActions(): void {
    this._actionsSub?.unsubscribe();
    this._actionsSub = this._api.getCaseActions(this._caseId)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({ next: actions => this.caseActions.set(actions), error: () => this.loadFailed.set(true) });
  }

  private getCaseActionsAndThenRequestModel(): void {
    this._actionsSub?.unsubscribe();
    this._actionsSub = this._api.getCaseActions(this._caseId)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(actions => {
        this.caseActions.set(actions);
        this.requestModel();
      });
  }

  protected getTimeline(): void {
    this._api.getCaseTimeline(this._caseId)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(entries => this.timelineEntries.set(entries));
  }

  private getRelatedCases(model: Case): void {
    if (!model.metadata?.['ExternalCorrelationKey']) {
      return;
    }
    this._api.getRelatedCases(this._caseId)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(related => this.relatedCases.set(related ?? []));
  }
}

