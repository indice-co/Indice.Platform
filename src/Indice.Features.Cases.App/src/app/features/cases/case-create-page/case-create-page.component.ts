import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CasesApiService, CaseTypePartial, Contact, ContactMeta, CreateDraftCaseRequest } from 'src/app/core/services/cases-api.service';
import { SearchContactComponent } from 'src/app/shared/components/search-contact/search-contact.component';
import { SelectCaseTypeComponent } from 'src/app/shared/components/select-case-type/select-case-type.component';
import { SIDE_PANE_OUTLET, SidePaneLayoutComponent } from 'src/app/ui/side-pane/side-pane';

/** Side pane: pick a case type and a customer, then create a draft case and open it. */
@Component({
  selector: 'app-case-create-page',
  imports: [TranslatePipe, SidePaneLayoutComponent, SelectCaseTypeComponent, SearchContactComponent],
  template: `
    <app-side-pane-layout [title]="'cases.newCase' | translate" [subtitle]="'cases.create.subtitle' | translate">
      <ol class="space-y-8">
        <li>
          <h3 class="mb-1 font-semibold">{{ 'cases.create.chooseType' | translate }}</h3>
          <p class="mb-3 text-sm text-base-content/70">{{ 'cases.create.chooseTypeHint' | translate }}</p>
          <app-select-case-type (selectedCaseType)="onCaseType($event)" />
        </li>
        @if (caseType(); as caseType) {
          <li>
            <h3 class="mb-1 font-semibold">{{ 'cases.create.findCustomer' | translate }}</h3>
            <p class="mb-3 text-sm text-base-content/70">{{ 'cases.create.findCustomerHint' | translate }}</p>
            <app-search-contact [caseTypeCode]="caseType.code" (selectedContact)="contact.set($event)" />
          </li>
        }
      </ol>
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-primary" [disabled]="!canCreate() || creating()" (click)="createDraft()">
          @if (creating()) { <span class="loading loading-spinner loading-xs"></span> }
          {{ 'cases.create.submit' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class CaseCreatePageComponent {
  private readonly _api = inject(CasesApiService);
  private readonly _router = inject(Router);

  protected readonly caseType = signal<CaseTypePartial | undefined>(undefined);
  protected readonly contact = signal<Contact | undefined>(undefined);
  protected readonly creating = signal(false);
  protected readonly canCreate = computed(() => !!this.caseType()?.code && !!this.contact());

  protected onCaseType(caseType: CaseTypePartial | undefined): void {
    this.caseType.set(caseType);
    this.contact.set(undefined);
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
    this.creating.set(true);
    this._api.createDraftAdminCase(request).subscribe({
      next: created => this._router.navigate(['/', { outlets: { primary: ['cases', created.id, 'details'], [SIDE_PANE_OUTLET]: null } }]),
      error: () => this.creating.set(false)
    });
  }
}
