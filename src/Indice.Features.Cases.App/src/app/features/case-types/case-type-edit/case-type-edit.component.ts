import { CaseTypeUpdateService } from '../case-type-update.service';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
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
    this.route.params.subscribe(params => {
      this.caseTypeId = params.caseTypeId;
      this._api.getCaseTypeById(this.caseTypeId).subscribe(caseType => {
        this.data = caseType;
      });
    });
  }

  onSubmit(event: any): void {
    this.caseTypesService.onEditSubmit(this.caseTypeId, event);
  }
}
