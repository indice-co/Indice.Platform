import { Component, effect, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideSearch, LucideUserRound } from '@lucide/angular';
import { CasesApiService, Contact } from 'src/app/core/services/cases-api.service';

/** Searches the contacts available for a case type and lets the user pick one. */
@Component({
  selector: 'app-search-contact',
  imports: [TranslatePipe, LucideSearch, LucideUserRound],
  template: `
    <form class="join w-full" (submit)="$event.preventDefault(); search()">
      <label class="input join-item w-full">
        <svg lucideSearch class="size-4 opacity-60"></svg>
        <input type="search" class="grow" name="searchValue" [placeholder]="'cases.searchContact.placeholder' | translate"
          [value]="term()" (input)="term.set($any($event.target).value)" />
      </label>
      <button type="submit" class="btn btn-primary join-item" [disabled]="searching()">
        @if (searching()) { <span class="loading loading-spinner loading-xs"></span> }
        {{ 'common.search' | translate }}
      </button>
    </form>

    @if (results(); as results) {
      @if (results.length) {
        <fieldset class="mt-4 space-y-2">
          <legend class="sr-only">{{ 'cases.searchContact.contact' | translate }}</legend>
          @for (contact of results; track contact.reference) {
            <label class="flex cursor-pointer items-start gap-3 rounded-box border px-4 py-3 transition-colors"
              [class]="contact === selected() ? 'border-primary bg-primary/5' : 'border-base-300 hover:bg-base-200'">
              <input type="radio" name="contact" class="radio radio-sm radio-primary mt-0.5" [checked]="contact === selected()" (change)="select(contact)" />
              <span class="min-w-0">
                <span class="block font-medium">{{ contact.firstName }} {{ contact.lastName }}</span>
                <span class="block text-sm text-base-content/70">
                  {{ 'cases.searchContact.referenceNumber' | translate }}: <span class="docket">{{ contact.reference }}</span>
                  @if (contact.tin) {
                    <span class="ml-2">{{ 'cases.searchContact.taxId' | translate }}: <span class="docket">{{ contact.tin }}</span></span>
                  }
                </span>
              </span>
            </label>
          }
        </fieldset>
      } @else {
        <div class="mt-4 flex items-center gap-3 rounded-box bg-base-200 px-4 py-3 text-sm text-base-content/70">
          <svg lucideUserRound class="size-4 shrink-0"></svg>
          {{ 'cases.searchContact.noResults' | translate }}
        </div>
      }
    }
  `
})
export class SearchContactComponent {
  private readonly _api = inject(CasesApiService);
  public readonly caseTypeCode = input<string>();
  public readonly selectedContact = output<Contact | undefined>();

  protected readonly term = signal('');
  protected readonly searching = signal(false);
  protected readonly results = signal<Contact[] | undefined>(undefined);
  protected readonly selected = signal<Contact | undefined>(undefined);

  constructor() {
    // a different case type means a different contact source: start over
    effect(() => {
      this.caseTypeCode();
      this.results.set(undefined);
      this.selected.set(undefined);
    });
  }

  protected search(): void {
    this.searching.set(true);
    this.selected.set(undefined);
    this.selectedContact.emit(undefined);
    this._api.getContacts(this.term() || undefined, this.caseTypeCode()).subscribe({
      next: result => {
        this.results.set(result.items ?? []);
        this.searching.set(false);
      },
      error: () => {
        this.results.set([]);
        this.searching.set(false);
      }
    });
  }

  protected select(contact: Contact): void {
    this.selected.set(contact);
    this.selectedContact.emit(contact);
  }
}
