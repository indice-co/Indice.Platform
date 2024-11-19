import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { tap } from 'rxjs/operators';
import { CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-related',
  templateUrl: './case-related.component.html'
})
export class RelatedCasesComponent implements OnInit {

  @Input() relatedCasesIds: string[] | undefined;
  currentCaseId: string = "";

  constructor(private api: CasesApiService, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.currentCaseId = params['caseId'];
    });
  }
}
