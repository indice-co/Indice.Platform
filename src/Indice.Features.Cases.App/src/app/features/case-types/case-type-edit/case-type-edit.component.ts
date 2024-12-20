import { CaseTypeUpdateService } from '../case-type-update.service';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map, switchMap, tap } from 'rxjs';
import { CasesApiService, CaseTypeRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-type-edit',
  templateUrl: './case-type-edit.component.html',
  styleUrls: ['./case-type-edit.component.scss']
})
export class CaseTypeEditComponent implements OnInit {

  private caseTypeId: string = '';

  public widgets = this.caseTypesService.widgets;

  public framework = this.caseTypesService.framework;

  public schema = this.caseTypesService.schema;

  public layout = this.caseTypesService.onLoadLayout(this.caseTypeId);

  public data: any;

  constructor(
    private route: ActivatedRoute,
    private _api: CasesApiService,
    private caseTypesService: CaseTypeUpdateService) { }

  ngOnInit(): void {
      this.route.params
          .pipe(tap(params => this.caseTypeId = params.caseTypeId),
                switchMap(params => this._api.getCaseTypeById(params.caseTypeId)))
          .pipe(tap(caseType => this.data = caseType))
          .subscribe();
  }

  onSubmit(event: any): void {
    this.caseTypesService.onEditSubmit(this.caseTypeId, event);
  }
}
