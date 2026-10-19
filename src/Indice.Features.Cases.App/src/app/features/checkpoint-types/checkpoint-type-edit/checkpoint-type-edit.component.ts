import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastType } from '@indice/ng-components';
import { EMPTY, Observable } from 'rxjs';
import { catchError, take } from 'rxjs/operators';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CaseStatus, CasesApiService, CaseTypePartial, CheckpointType, CheckpointTypeRequest } from 'src/app/core/services/cases-api.service';
import { TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';

@Component({
    selector: 'app-checkpoint-type-edit',
    templateUrl: './checkpoint-type-edit.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CheckpointTypeEditComponent implements OnInit {
  public checkpointTypeId: string | undefined;
  public model = new CheckpointTypeRequest({ caseTypeId: '', code: '', status: CaseStatus.Submitted, private: false });
  public caseTypes: CaseTypePartial[] = [];
  public statuses = Object.values(CaseStatus);
  public busy = false;

  constructor(
    private _route: ActivatedRoute,
    private _router: Router,
    private _api: CasesApiService,
    private _caseTypeService: CaseTypeService,
    private _toaster: TranslatedToasterService) { }

  public get isEdit(): boolean {
    return !!this.checkpointTypeId;
  }

  ngOnInit(): void {
    this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe(result => this.caseTypes = result.items ?? []);
    this.checkpointTypeId = this._route.snapshot.paramMap.get('checkpointTypeId') ?? undefined;
    const caseTypeId = this._route.snapshot.queryParamMap.get('caseTypeId');
    if (caseTypeId) {
      this.model.caseTypeId = caseTypeId;
    }
    if (this.checkpointTypeId) {
      this.busy = true;
      this._api.getCheckpointTypeById(this.checkpointTypeId).subscribe((item: CheckpointType) => {
        this.model = new CheckpointTypeRequest({
          caseTypeId: item.caseTypeId!,
          code: item.code!,
          title: item.title,
          description: item.description,
          status: item.status,
          private: item.private,
          translations: item.translations
        });
        this.busy = false;
      });
    }
  }

  public submit(): void {
    this.busy = true;
    const request$: Observable<CheckpointType> = this.isEdit
      ? this._api.updateCheckpointType(this.checkpointTypeId!, this.model)
      : this._api.createCheckpointType(this.model);
    request$.pipe(
      catchError(err => {
        this.busy = false;
        this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(_ => {
      this.busy = false;
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.checkpointTypeSaved.body');
      this.goBack();
    });
  }

  public goBack(): void {
    this._router.navigate(['/checkpoint-types'], { queryParams: this.model.caseTypeId ? { caseTypeId: this.model.caseTypeId } : {} });
  }
}
