import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseListComponent, Icons, IResultSet, ModalService, RouterViewAction, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { catchError, EMPTY, map, Observable, take, tap } from 'rxjs';
import { CasesApiService, CaseStatus, CheckpointType, CheckpointTypeResultSet, CheckpointTypeRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-checkpoint-types-component',
  templateUrl: './checkpoint-types.component.html'
})

export class CheckpointTypesComponent extends BaseListComponent<CheckpointType> implements OnInit {
  public newItemLink = 'create-checkpoint-type';

  public formActions: ViewAction[] = [
    new RouterViewAction(Icons.Add, this.newItemLink, 'rightpane', 'Create new checkpoint', 'Create new checkpoint')
  ];
  public checkpointTypes: CheckpointType[] = [];
  public isModalOpen: boolean = false;
  public newCheckpointType: CheckpointTypeRequest = {};
  public statuses = Object.values(CaseStatus);
  private caseTypeId: string = '';
  public queryParamsHasFilter = false;

  constructor(
    private route: ActivatedRoute,
    private _api: CasesApiService,
    private toaster: ToasterService,
    private router: Router,
    protected _modalService: ModalService,
  ) {
    super(route, router);
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.caseTypeId = params.caseTypeId;
      super.ngOnInit();
    });
  }

  submitForm(): void {
    this._api.createCheckpointType(undefined, this.newCheckpointType).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, "Success", "Checkpoint type created successfully");
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, "Error", err.message);
        return EMPTY;
      })
    ).subscribe();
  }

  loadItems(): Observable<IResultSet<CheckpointType> | null | undefined> {
    return this._api.getCaseTypeCheckpointTypes(this.caseTypeId)
    .pipe(
      take(1),
      map((result: CheckpointTypeResultSet) => (result as IResultSet<CheckpointType>))
    );
  }
}
