import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { catchError, EMPTY, take, tap } from 'rxjs';
import { CasesApiService, CaseStatus, CheckpointType, EditCheckpointTypeRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-checkpoint-type-edit-component',
  templateUrl: './checkpoint-type-edit.component.html'
})

export class CheckpointTypeEditComponent implements OnInit {
  public checkpointTypes: CheckpointType[] = [];
  public title: string = "";
  public checkpointTypeToEdit: Partial<CheckpointType> = {};
  public statuses = Object.values(CaseStatus);
  public loadedCheckpoint: LoadedCheckpoint = new LoadedCheckpoint();

  constructor(
    private route: ActivatedRoute,
    private _api: CasesApiService,
    private toaster: ToasterService,
    private router: Router
  ) { }

  ngOnInit(): void {
    const urlTree = this.router.parseUrl(this.router.url);
    const checkpointId = urlTree.root.children['rightpane'].segments[0].path;
    this.loadCheckpointTypeById(checkpointId);
  }

  loadCheckpointTypeById(checkpointId: string): void {
    this._api.getCheckpointTypeById(checkpointId).subscribe(checkpointType => {
      this.loadedCheckpoint = {
        id: checkpointType.id,
        code: checkpointType.code,
        title: checkpointType.title,
        description: checkpointType.description,
        status: checkpointType.status,
        private: checkpointType.private,
        translations: checkpointType.translations
      };
    });
  }

  editCheckpoint() {
    const caseTypeId = this.router.url.split("/")[2];
    const editCheckpointRequest: EditCheckpointTypeRequest = {
      checkpointTypeId: this.loadedCheckpoint.id,
      code: this.loadedCheckpoint.code,
      caseTypeId: caseTypeId
    }

    this._api.editCheckpointType(undefined, editCheckpointRequest).pipe(
      tap(_ => {
        this.toaster.show(ToastType.Success, "Success", "Checkpoint type created successfully");
      }),
      catchError(err => {
        this.toaster.show(ToastType.Error, "Error", err.detail);
        return EMPTY;
      }),
      take(1)
    ).subscribe();
  }
}

class LoadedCheckpoint {
  id?: string;
  code?: string | undefined;
  title?: string | undefined;
  description?: string | undefined;
  status?: CaseStatus;
  private?: boolean | undefined;
  translations?: string | undefined;
}
