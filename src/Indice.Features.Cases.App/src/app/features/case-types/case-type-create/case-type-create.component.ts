
import { Component, OnInit } from '@angular/core';
import { CaseTypesService } from '../case-types.service';

@Component({
  selector: 'app-case-type-create',
  templateUrl: './case-type-create.component.html',
  styleUrls: ['./case-type-create.component.scss']
})
export class CaseTypeCreateComponent implements OnInit {

  public widgets = this.caseTypesService.widgets;

  public framework = this.caseTypesService.framework;

  public schema = this.caseTypesService.schema;

  public layout = this.caseTypesService.onLoadLayout();

  constructor(private caseTypesService: CaseTypesService) { }

  ngOnInit(): void {
    
  }

  onSubmit(event: any) {
    this.caseTypesService.onCreateSubmit(event);
  }

}
