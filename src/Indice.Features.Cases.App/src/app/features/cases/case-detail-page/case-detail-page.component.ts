import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { iif, Observable, ReplaySubject, of } from 'rxjs';
import { map, switchMap, takeUntil, tap } from 'rxjs/operators';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { CaseActions, Case, CasesApiService, ActionRequest, TimelineEntry, CaseStatus, SuccessMessage } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-detail-page',
  templateUrl: './case-detail-page.component.html'
})
export class CaseDetailPageComponent implements OnInit, OnDestroy {

  public model$: Observable<Case> | undefined;

  private _caseActions: ReplaySubject<CaseActions> = new ReplaySubject(1);
  public caseActions$ = this._caseActions.asObservable();

  public timelineEntries$: Observable<TimelineEntry[]> | undefined;

  public formValid: boolean = false;
  public formUnSavedChanges: boolean = false;

  private componentDestroy$ = new ReplaySubject<void>(1);

  private caseId = '';
  public customDataValid = true;
  public showCustomDataValidation = false;
  public now: Date = new Date();
  public typeId?: string;
  public caseTypeConfig: any;
  public caseStatus = CaseStatus;

  /** shows the warning modal conditionally */
  public showWarningModal: boolean = false;
  public warningModalState = { title: 'Έγκριση αίτησης', description: 'Δεν έχετε τυπώσει το PDF της αίτησης, θέλετε να προχωρήσετε στην έγκρισή της;' };

  constructor(
    private api: CasesApiService,
    private caseDetailsService: CaseDetailsService,
    private route: ActivatedRoute,
    private router: Router,
    private toaster: ToasterService) { }

  ngOnInit(): void {
    this.route.params.subscribe(p => {
      this.caseId = p.caseId;
      this.requestModel();
      this.getCaseActions();
      this.getTimeline();
    });
  }

  ngOnDestroy(): void {
    this.componentDestroy$.complete();
  }

  public updateData(event: { draft: boolean }): void {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
    this.showWarningModal = this.caseTypeConfig?.boOptions?.showWarningModal === false ? false : true;
  }

  public isValid(event: boolean): void {
    this.formValid = event;
  }

  public formDataHasChanged(event: boolean): void {
    this.formUnSavedChanges = event;
  }

  public requestModel(): void {
    this.api
      .getCaseById(this.caseId)
      .pipe(
        switchMap(caseDetails => {
          return iif(
            () => caseDetails.draft === true,
            this.getCustomerData$(caseDetails), // draft mode, we need to prefill the form with customer data (if any)
            of(caseDetails))
        }),
        tap((response: Case) => {
          this.caseTypeConfig = response.caseType?.config ? JSON.parse(response.caseType?.config) : {};
          this.caseDetailsService.setCaseDetails(response);
          // ensure that we have the correct "latest" caseDetails!
          this.model$ = this.caseDetailsService.caseDetails$;
        }),
        takeUntil(this.componentDestroy$)
      ).subscribe();

  }
  private getCustomerData$(caseDetails: Case): Observable<Case> {
    return this.api
      .getCustomerData(caseDetails.customerId ?? "", caseDetails.caseType?.code ?? "")
      .pipe(
        map(customerDetails => {
          caseDetails.data = customerDetails.formData;
          return caseDetails;
        }));

  }

  onActionsChanged() {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
  }

  onCaseDiscarded() {
    this.toaster.show(ToastType.Info, 'Ακύρωση αίτησης', 'Η αίτηση έχει ακυρωθεί');
    this.router.navigate(['/cases']);
  }

  /**
   * Event for PDF print action, 
   * registers the state of the PDF print action
   * @param printed 
   * @returns 
   */
  onPdfButtonClicked(printed: boolean | undefined) {
    if (printed === undefined) {
      return;
    }
    this.showWarningModal = this.caseTypeConfig?.boOptions?.showWarningModal === false ? false : !printed;
  }

  /**
   * Trigger a blocking workflow activity by its Id.
   * @param event The action Id to trigger the corresponding custom workflow action.
   */
  onCustomActionTrigger(event: { redirectToList: boolean | undefined, successMessage: SuccessMessage | undefined, id: string | undefined, value: string | undefined }) {
    this.api.triggerAction(this.caseId, undefined, new ActionRequest({ id: event?.id, value: event?.value }))
      .pipe(
        tap(() => {
          if (event.redirectToList) {
            if (event.successMessage) {
              this.toaster.show(ToastType.Info, event.successMessage.title, event.successMessage.body);
            }
            this.router.navigate(['/cases']);
          } else {
            this.onActionsChanged();
          }
        })
      )
      .subscribe();
  }

  private getCaseActions() {
    this.api.getCaseActions(this.caseId)
      .pipe(
        tap(response => this._caseActions.next(response)),
        takeUntil(this.componentDestroy$)
      )
      .subscribe();
  }

  private getCaseActionsAndThenRequestModel() {
    this.api.getCaseActions(this.caseId)
      .pipe(
        tap(response => {
          this._caseActions.next(response);
          this.requestModel();
        }),
        takeUntil(this.componentDestroy$)
      )
      .subscribe();
  }

  private getTimeline() {
    this.timelineEntries$ = this.api.getCaseTimeline(this.caseId!);
  }

}