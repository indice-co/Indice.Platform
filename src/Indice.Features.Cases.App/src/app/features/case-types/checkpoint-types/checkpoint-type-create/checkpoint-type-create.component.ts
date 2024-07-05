import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { catchError, EMPTY, tap } from 'rxjs';
import { CasesApiService, CaseStatus, CheckpointType, CheckpointTypeRequest } from 'src/app/core/services/cases-api.service';


@Component({
  selector: 'app-checkpoint-type-create-component',
  templateUrl: './checkpoint-type-create.component.html'
})

export class CheckpointTypeCreateComponent implements OnInit {
  public checkpointTypes: CheckpointType[] = [];
  public isModalOpen: boolean = false;
  public newCheckpointType: CheckpointTypeRequest = {};
  public statuses = Object.values(CaseStatus);
  public title?: string;

  constructor(
    private route: ActivatedRoute,
    private _api: CasesApiService,
    private toaster: ToasterService,
    private router: Router
  ) { }

  ngOnInit(): void {
  }

  createCheckpoint(): void {
    const caseTypeId = this.router.url.split("/")[2];
    const newCheckpoint: CheckpointTypeRequest = {
      caseTypeId: caseTypeId,
      code: this.newCheckpointType.code,
      title: this.newCheckpointType.title,
      description: this.newCheckpointType.description,
      status: this.newCheckpointType.status,
      private: this.newCheckpointType.private,
      translations: this.newCheckpointType.translations,
    };

    this._api.createCheckpointType(undefined, newCheckpoint).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, "Success", "Checkpoint type created successfully");
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, "Error", err.detail);
        return EMPTY;
      })
    ).subscribe();
  }
}

