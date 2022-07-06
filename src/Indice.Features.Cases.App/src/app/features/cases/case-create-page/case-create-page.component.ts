import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { CasesApiService, CreateDraftCaseRequest, CustomerDetails, CustomerMeta, ICaseType } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-create-page',
  templateUrl: './case-create-page.component.html',
  styleUrls: ['./case-create-page.component.scss']
})
export class CaseCreatePageComponent implements OnInit {
  public customer?: CustomerDetails;
  public caseTypeCode?: string;

  constructor(
    private api: CasesApiService,
    private router: Router) {

  }

  ngOnInit(): void {

  }

  public createDraft() {
    const request = new CreateDraftCaseRequest({
      caseTypeCode: this.caseTypeCode,
      customer: new CustomerMeta({
        customerId: this.customer?.customerId,
        firstName: this.customer?.firstName,
        lastName: this.customer?.lastName,
        userId: this.customer?.userId
      }),
      groupId: this.customer?.groupId,
      metadata: this.customer?.metadata
    });

    this.api.createDraftAdminCase(undefined, request)
      .pipe(
        tap(caseId => {
          this.router.navigate([`cases/${caseId}`]);
        })
      )
      .subscribe()
  }
}
