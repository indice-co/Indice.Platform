import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CasePartial } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-related',
  templateUrl: './related-cases.component.html'
})
export class RelatedCasesComponent implements OnInit {

  @Input() relatedCases: CasePartial[] = [];
  currentCaseId: string = "";

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    // From the related cases, remove the currently opened case from the list
    this.route.paramMap.subscribe(params => {
      this.currentCaseId = params.get('caseId') ?? "";
    });
  }
}
