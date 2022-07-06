import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable, ReplaySubject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';
import { CaseActions, CaseDetails, CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-detail-page',
  templateUrl: './case-detail-page.component.html'
})
export class CaseDetailPageComponent implements OnInit, OnDestroy {

  private _model: ReplaySubject<CaseDetails> = new ReplaySubject(1);
  public model$ = this._model.asObservable();

  private _caseActions: ReplaySubject<CaseActions> = new ReplaySubject(1);
  public caseActions$ = this._caseActions.asObservable();

  public timelineEntries$: Observable<TimelineEntry[]> | undefined;

  private componentDestroy$ = new ReplaySubject<void>(1);

  private caseId = '';
  public customDataValid = true;
  public showCustomDataValidation = false;
  public now: Date = new Date();
  public typeId?: string;

  constructor(
    private api: CasesApiService,
    private route: ActivatedRoute) { }

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

  public updateData(): void {
    this.requestModel();
    this.getTimeline();
  }

  public requestModel(): void {
    this.api
      .getCaseById(this.caseId)
      .pipe(
        tap(response => this._model.next(response)),
        takeUntil(this.componentDestroy$)
      )
      .subscribe();
  }

  onActionsChanged() {
    this.getCaseActionsAndThenRequestModel();
    this.getTimeline();
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
