import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { CasesApiService, CreateDraftCaseRequest, Contact, ContactMeta } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-create-page',
  templateUrl: './case-create-page.component.html',
  styleUrls: ['./case-create-page.component.scss']
})
export class CaseCreatePageComponent implements OnInit {
  public contact?: Contact;
  public caseTypeCode?: string;
  public title?: string;

  constructor(
    private api: CasesApiService,
    private router: Router) {

  }

  ngOnInit(): void { }

  public createDraft() {
    const request = new CreateDraftCaseRequest({
      caseTypeCode: this.caseTypeCode,
      owner: new ContactMeta({
        reference: this.contact?.reference,
        firstName: this.contact?.firstName,
        lastName: this.contact?.lastName,
        userId: this.contact?.userId
      }),
      groupId: this.contact?.groupId,
      metadata: this.contact?.metadata
    });

    this.api.createDraftAdminCase(request)
      .pipe(
        tap(caseCreated => {
            this.router.navigate([`cases/${caseCreated.id}`]);
        })
      )
      .subscribe()
  }
}
