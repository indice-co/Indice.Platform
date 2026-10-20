import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { map } from 'rxjs/operators';
import { CasesApiService, CaseTypePartial, Contact, ContactMeta, CreateDraftCaseRequest } from 'src/app/core/services/cases-api.service';
import { EmptyStateComponent } from 'src/app/ui/feedback/empty-state.component';
import { SidePaneComponent } from 'src/app/ui/side-pane/side-pane.component';

interface CaseTypeGroup {
  name: string;
  caseTypes: CaseTypePartial[];
}

/** New case: pick a case type, find the customer, then a draft case is created and opened. */
@Component({
  selector: 'app-case-create-page',
  imports: [FormsModule, NgIcon, TranslatePipe, SidePaneComponent, EmptyStateComponent],
  templateUrl: './case-create-page.component.html'
})
export class CaseCreatePageComponent {
  private readonly api = inject(CasesApiService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  protected readonly caseType = signal<CaseTypePartial | undefined>(undefined);
  protected readonly contact = signal<Contact | undefined>(undefined);
  protected readonly searchValue = signal('');
  /** The search that produced the current results (results load only when the user searches). */
  private readonly searchedFor = signal<{ term: string; caseTypeCode: string } | undefined>(undefined);

  /** Case types the user may create cases for, grouped by category in server order. */
  protected readonly caseTypes = rxResource({
    stream: () => this.api.getCaseTypesList(true).pipe(map(result => this.groupByCategory(result.items ?? [])))
  });

  protected readonly contacts = rxResource({
    params: () => this.searchedFor(),
    stream: ({ params }) => this.api.getContacts(params.term, params.caseTypeCode).pipe(map(result => result.items ?? []))
  });

  protected readonly step = computed(() => this.caseType() ? 2 : 1);

  protected selectCaseType(caseType: CaseTypePartial | undefined): void {
    this.caseType.set(caseType);
    // Changing the case type invalidates the chosen customer.
    this.contact.set(undefined);
    this.searchedFor.set(undefined);
  }

  protected search(): void {
    this.contact.set(undefined);
    this.searchedFor.set({ term: this.searchValue(), caseTypeCode: this.caseType()!.code! });
  }

  protected createDraft(): void {
    const contact = this.contact();
    const request = new CreateDraftCaseRequest({
      caseTypeCode: this.caseType()?.code,
      owner: new ContactMeta({
        reference: contact?.reference,
        firstName: contact?.firstName,
        lastName: contact?.lastName,
        userId: contact?.userId,
        tin: contact?.tin
      }),
      groupId: contact?.groupId,
      metadata: contact?.metadata
    });
    this.api.createDraftAdminCase(request).subscribe(caseCreated => this.router.navigate([`cases/${caseCreated.id}`]));
  }

  private groupByCategory(caseTypes: CaseTypePartial[]): CaseTypeGroup[] {
    const otherCategory = this.translate.instant('notifications.otherCategory');
    const groups = new Map<string, CaseTypeGroup>();
    for (const caseType of caseTypes) {
      const name = caseType?.category?.name ?? otherCategory;
      if (!groups.has(name)) {
        groups.set(name, { name, caseTypes: [] });
      }
      groups.get(name)!.caseTypes.push(caseType);
    }
    return [...groups.values()];
  }
}
