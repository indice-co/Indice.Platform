import { DatePipe, formatDate, NgTemplateOutlet } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { iif, Observable, of, Subscription } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { ActionRequest, Case, CaseActions, CasePartial, CasesApiService, CaseStatus, TimelineEntry } from 'src/app/core/services/cases-api.service';
import { ConfirmDialogComponent, ConfirmDialogData } from 'src/app/ui/feedback/confirm-dialog.component';
import { DialogService } from 'src/app/ui/feedback/dialog.service';
import { ToastService, ToastType } from 'src/app/ui/feedback/toast.service';
import { PersonComponent } from 'src/app/ui/people/person.component';
import { StatusStampComponent } from 'src/app/ui/status/case-status';
import { CaseCustomActionComponent, CustomActionTrigger } from './case-custom-action/case-custom-action.component';
import { CaseDecisionComponent } from './case-decision/case-decision.component';
import { CaseFormComponent } from './case-form/case-form.component';
import { CaseTimelineComponent } from './case-timeline/case-timeline.component';
import { RelatedCasesComponent } from './related-cases/related-cases.component';

interface Fact {
  /** Translation key. */
  label: string;
  value: string | null | undefined;
  mono?: boolean;
}

/**
 * One case: its data form on the left, and on the right what can be done with it (assign, decide, run workflow actions,
 * download, discard a draft), its details, related cases and timeline.
 */
@Component({
  selector: 'app-case-detail-page',
  imports: [
    DatePipe, NgTemplateOutlet, RouterLink, NgIcon, TranslatePipe, StatusStampComponent, PersonComponent,
    CaseFormComponent, CaseDecisionComponent, CaseCustomActionComponent, CaseTimelineComponent, RelatedCasesComponent
  ],
  templateUrl: './case-detail-page.component.html'
})
export class CaseDetailPageComponent implements OnInit {
  private readonly api = inject(CasesApiService);
  private readonly caseDetailsService = inject(CaseDetailsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly toaster = inject(ToastService);
  private readonly dialogs = inject(DialogService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly model = signal<Case | undefined>(undefined);
  protected readonly caseActions = signal<CaseActions | undefined>(undefined);
  protected readonly timelineEntries = signal<TimelineEntry[] | undefined>(undefined);
  protected readonly relatedCases = signal<CasePartial[]>([]);
  protected readonly formValid = signal(false);
  protected readonly formUnSavedChanges = signal(false);
  protected readonly assigning = signal(false);
  protected caseTypeConfig: any = {};
  /** Ask for confirmation before approving (case type option, or until the PDF has been downloaded). */
  protected readonly showWarningModal = signal(false);
  private caseId = '';
  private modelSub: Subscription | undefined;

  protected readonly basicFacts = computed<Fact[]>(() => {
    const model = this.model();
    return [
      { label: 'caseDetails.referenceNumber', value: model?.referenceNumber?.toString(), mono: true },
      { label: 'caseDetails.submitDate', value: model?.createdByWhen ? formatDate(model.createdByWhen, 'dd/MM/yyyy', 'en-US') : undefined },
      { label: 'caseDetails.caseType', value: model?.caseType?.title },
      { label: 'caseDetails.channel', value: model?.channel },
      { label: 'caseDetails.checkpoint', value: model?.checkpointType?.title ?? model?.checkpointType?.code },
      { label: 'caseDetails.assignedTo', value: model?.assignedToName || ' - ' },
      { label: 'caseDetails.caseId', value: model?.id, mono: true }
    ];
  });

  protected readonly customerFacts = computed<Fact[]>(() => {
    const model = this.model();
    return [
      { label: 'caseDetails.customerName', value: model?.ownerName },
      { label: 'caseDetails.customerId', value: model?.ownerId, mono: true },
      { label: 'caseDetails.taxId', value: model?.ownerTin, mono: true },
      { label: 'caseDetails.groupId', value: model?.groupId, mono: true }
    ];
  });

  protected readonly userFacts = computed<Fact[]>(() => {
    const metadata = this.model()?.metadata;
    return [
      { label: 'caseDetails.userSurname', value: metadata?.['UserSurname'] },
      { label: 'caseDetails.userFirstName', value: metadata?.['UserFirstName'] },
      { label: 'caseDetails.userRole', value: metadata?.['UserRole'] },
      { label: 'caseDetails.userCustomerId', value: metadata?.['UserCustomerId'], mono: true }
    ];
  });

  /** The PDF can be downloaded at the checkpoints the case type allows, otherwise once the case is in progress or completed. */
  protected readonly canDownloadPdf = computed(() => {
    const model = this.model();
    const allowed: string[] | undefined = this.caseTypeConfig?.checkpointsThatAllowDownload;
    return allowed
      ? allowed.includes(model?.checkpointType?.code!)
      : (model?.checkpointType?.status === CaseStatus.Completed || model?.checkpointType?.status === CaseStatus.InProgress);
  });

  /** Built on read so it follows the current language. */
  protected get warningModalState(): { title: string; description: string } {
    return {
      title: this.translate.instant('caseDetails.approveWarning.title'),
      description: this.translate.instant('caseDetails.approveWarning.description')
    };
  }

  public ngOnInit(): void {
    this.route.params
      .pipe(map(params => params['caseId']), takeUntilDestroyed(this.destroyRef))
      .subscribe(caseId => {
        this.caseId = caseId;
        this.model.set(undefined);
        this.caseActions.set(undefined);
        this.relatedCases.set([]);
        this.requestModel(true);
        this.getCaseActions();
        this.getTimeline();
      });
  }

  protected updateData(): void {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
    this.showWarningModal.set(this.caseTypeConfig?.boOptions?.showWarningModal === false ? false : true);
  }

  protected onActionsChanged(): void {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
  }

  protected assignToMe(): void {
    this.assigning.set(true);
    this.api.assignCase(this.caseId).subscribe({
      next: () => {
        this.assigning.set(false);
        this.onActionsChanged();
      },
      error: () => this.assigning.set(false)
    });
  }

  protected discardDraft(): void {
    const data: ConfirmDialogData = {
      title: 'caseDetails.discardDraft.title',
      message: 'caseDetails.discardDraft.confirm',
      confirmLabel: 'caseDetails.discardDraft.title',
      tone: 'danger'
    };
    this.dialogs.open<ConfirmDialogComponent, boolean>(ConfirmDialogComponent, { data }).closed.subscribe(confirmed => {
      if (!confirmed) {
        return;
      }
      this.api.deleteDraftCase(this.caseId).subscribe(() => {
        this.toaster.show(ToastType.Info, 'caseDetails.discardDraft.title', 'toasts.caseDiscarded.body');
        this.router.navigate(['/cases']);
      });
    });
  }

  /** Opens the case PDF. Once it has been downloaded, approving no longer asks for confirmation. */
  protected downloadPdf(): void {
    this.api.downloadCasePdf(this.caseId).subscribe(results => {
      const fileURL = window.URL.createObjectURL(results.data);
      this.showWarningModal.set(false);
      window.open(fileURL, '_blank');
    });
  }

  /** Triggers a blocking workflow activity by its id. */
  protected onCustomActionTrigger(event: CustomActionTrigger): void {
    this.api.triggerAction(this.caseId, new ActionRequest({ id: event?.id, value: event?.value })).subscribe(() => {
      if (event.redirectToList) {
        if (event.successMessage) {
          this.toaster.show(ToastType.Info, event.successMessage.title, event.successMessage.body);
        }
        this.router.navigate(['/cases']);
      } else {
        this.onActionsChanged();
      }
    });
  }

  private requestModel(withRelatedCases = false): void {
    this.modelSub?.unsubscribe();
    this.modelSub = this.api.getCaseById(this.caseId)
      .pipe(
        // A draft's form is prefilled with the data the server initialises for it.
        switchMap(caseDetails => iif(() => caseDetails.draft === true, this.initializeData$(caseDetails), of(caseDetails))),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(response => {
        this.caseTypeConfig = response.caseType?.config ? response.caseType.config : {};
        this.caseDetailsService.setCaseDetails(response);
        this.model.set(response);
        if (withRelatedCases && response.metadata?.['ExternalCorrelationKey']) {
          this.api.getRelatedCases(this.caseId).subscribe(related => this.relatedCases.set(related));
        }
      });
  }

  private initializeData$(caseDetails: Case): Observable<Case> {
    return this.api.initializeCaseData(caseDetails.id!).pipe(map(contactData => {
      caseDetails.data = contactData;
      return caseDetails;
    }));
  }

  private getCaseActions(): void {
    this.api.getCaseActions(this.caseId).subscribe(response => this.caseActions.set(response));
  }

  private getCaseActionsAndThenRequestModel(): void {
    this.api.getCaseActions(this.caseId).subscribe(response => {
      this.caseActions.set(response);
      this.requestModel();
    });
  }

  private getTimeline(): void {
    this.api.getCaseTimeline(this.caseId).subscribe(entries => this.timelineEntries.set(entries));
  }
}
