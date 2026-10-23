import { Component, ElementRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, catchError, debounceTime, map, merge, of, switchMap, tap } from 'rxjs';
import { LucideCheck, LucideChevronDown, LucideInbox, LucideMail, LucidePencil, LucidePhone, LucideSearch, LucideTrash, LucideUserPlus } from '@lucide/angular';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactAvatarComponent, contactDisplayName } from './contact-avatar.component';

interface SelectedContact {
  key: number;
  contact: Contact;
  editing: boolean;
  isNew: boolean;
}

type EditableField = 'salutation' | 'firstName' | 'lastName' | 'fullName' | 'email' | 'phoneNumber' | 'recipientId';

const PAGE_SIZE = 6;
const EMAIL_PATTERN = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
let nextId = 0;

/**
 * Picks existing contacts through a searchable combobox, or drafts new ones when a search has no results.
 * The host calls `submit()` (emits `onSubmit` with the picked contacts), `cancel()` (restores the last submitted
 * selection and emits `onCancel`) or `reset()`.
 */
@Component({
  selector: 'app-list-contact-create',
  imports: [
    TranslatePipe, ContactAvatarComponent,
    LucideCheck, LucideChevronDown, LucideInbox, LucideMail, LucidePencil, LucidePhone, LucideSearch, LucideTrash, LucideUserPlus
  ],
  host: { class: 'block' },
  templateUrl: './list-contact-create.component.html'
})
export class ListContactCreateComponent {
  private readonly _api = inject(MessagesApiClient);
  private readonly _input = viewChild.required<ElementRef<HTMLInputElement>>('searchInput');

  public readonly onSubmit = output<Contact[]>();
  public readonly onCancel = output<Contact[]>();

  protected readonly listboxId = `contact-combobox-${nextId++}`;
  protected readonly fieldId = `${this.listboxId}-input`;
  protected readonly displayName = contactDisplayName;
  protected readonly fields: { name: EditableField; label: string; placeholder: string; info?: string }[] = [
    { name: 'salutation', label: 'Shared.SalutationLabel', placeholder: 'Shared.SalutationPlaceholder', info: 'Shared.SalutationInfo' },
    { name: 'firstName', label: 'Shared.FirstNameLabel', placeholder: 'Shared.FirstNamePlaceholder' },
    { name: 'lastName', label: 'Shared.LastNameLabel', placeholder: 'Shared.LastNamePlaceholder' },
    { name: 'fullName', label: 'Shared.DisplayNameLabel', placeholder: 'Shared.DisplayNamePlaceholder' },
    { name: 'email', label: 'Shared.EmailLabel', placeholder: 'Shared.EmailPlaceholder' },
    { name: 'phoneNumber', label: 'Shared.PhoneLabel', placeholder: 'Shared.PhonePlaceholder' },
    { name: 'recipientId', label: 'Shared.ContactCodeLabel', placeholder: 'Shared.ContactCodePlaceholder' }
  ];

  protected readonly term = signal('');
  protected readonly open = signal(false);
  protected readonly loading = signal(false);
  protected readonly results = signal<Contact[]>([]);
  protected readonly hasMore = signal(false);
  protected readonly activeIndex = signal(-1);
  private readonly _selected = signal<SelectedContact[]>([]);
  /** The contacts picked so far. */
  public readonly selected = this._selected.asReadonly();

  /** No search results for a non empty term: the only option is "create contact". */
  protected readonly canCreate = computed(() => !this.loading() && this.results().length === 0 && this.term().trim().length > 0);
  protected readonly optionCount = computed(() => this.canCreate() ? 1 : this.results().length + (this.hasMore() ? 1 : 0));
  protected readonly activeId = computed(() => this.open() && this.activeIndex() >= 0 ? this.optionId(this.activeIndex()) : null);
  protected readonly selectedIds = computed(() => new Set(this._selected().map(item => item.contact.id).filter(Boolean)));

  private readonly _term$ = new Subject<string>();
  private readonly _more$ = new Subject<void>();
  private _page = 1;
  private _searched = false;
  private _saved: SelectedContact[] = [];
  private _nextKey = 0;

  constructor() {
    const firstPage$ = this._term$.pipe(debounceTime(300), map(term => ({ term, page: 1 })));
    const nextPage$ = this._more$.pipe(map(() => ({ term: this.term().trim(), page: this._page + 1 })));
    merge(firstPage$, nextPage$)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(({ term, page }) => this._api
          .getContacts(page, PAGE_SIZE, 'email', term || undefined, undefined, undefined, undefined, undefined, undefined, true)
          .pipe(
            map(result => ({ items: result.items ?? [], page })),
            catchError(() => of({ items: [] as Contact[], page }))
          )),
        takeUntilDestroyed()
      )
      .subscribe(({ items, page }) => {
        this._page = page;
        this.results.update(previous => page === 1 ? items : [...previous, ...items]);
        this.hasMore.set(items.length === PAGE_SIZE);
        this.loading.set(false);
        if (page === 1) {
          this.activeIndex.set(this.optionCount() > 0 ? 0 : -1);
        }
      });
  }

  // --- public API (also used by the campaign recipients step) ---

  public submit(): void {
    this._saved = this._selected().map(item => ({ ...item, editing: false }));
    this.onSubmit.emit(this.contacts());
  }

  public cancel(): void {
    this._selected.set(this._saved.map(item => ({ ...item })));
    this.onCancel.emit(this.contacts());
  }

  public reset(): void {
    this._selected.set([]);
    this._saved = [];
    this.term.set('');
    this.results.set([]);
    this.open.set(false);
    this._searched = false;
  }

  // --- combobox ---

  protected optionId(index: number): string {
    return `${this.listboxId}-option-${index}`;
  }

  protected onInput(value: string): void {
    this.term.set(value);
    this.open.set(true);
    this._searched = true;
    this._term$.next(value.trim());
  }

  protected openList(): void {
    this.open.set(true);
    if (!this._searched) {
      this._searched = true;
      this._term$.next(this.term().trim());
    }
  }

  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (!next || !(event.currentTarget as HTMLElement).contains(next)) {
      this.open.set(false);
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    const count = this.optionCount();
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.open()) {
          this.openList();
          return;
        }
        this.moveActive(count ? (this.activeIndex() + 1) % count : -1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActive(count ? (this.activeIndex() - 1 + count) % count : -1);
        break;
      case 'Home':
      case 'End':
        if (this.open() && count) {
          event.preventDefault();
          this.moveActive(event.key === 'Home' ? 0 : count - 1);
        }
        break;
      case 'Enter':
        event.preventDefault();
        if (this.open()) {
          this.activate(this.activeIndex());
        }
        break;
      case 'Escape':
        if (this.open()) {
          event.preventDefault();
          event.stopPropagation();
          this.open.set(false);
        }
        break;
      case 'Tab':
        this.open.set(false);
        break;
    }
  }

  protected activate(index: number): void {
    if (index < 0) {
      return;
    }
    if (this.canCreate()) {
      this.createFromTerm();
    } else if (index < this.results().length) {
      this.pick(this.results()[index]);
    } else if (this.hasMore()) {
      this.showMore();
    }
  }

  protected showMore(): void {
    if (!this.loading()) {
      this._more$.next();
    }
  }

  protected pick(contact: Contact): void {
    if (contact.id && this.selectedIds().has(contact.id)) {
      return;
    }
    this._selected.update(items => [...items, { key: this._nextKey++, contact: new Contact({ ...contact }), editing: false, isNew: false }]);
  }

  protected createFromTerm(): void {
    const term = this.term().trim();
    if (!term || this._selected().some(item => item.contact.fullName === term)) {
      return;
    }
    const contact = new Contact();
    if (EMAIL_PATTERN.test(term.toLowerCase())) {
      contact.email = term;
    } else {
      contact.fullName = term;
      contact.firstName = term.split(' ')[0];
      contact.lastName = term.slice(contact.firstName.length).trim();
    }
    this._selected.update(items => [{ key: this._nextKey++, contact, editing: true, isNew: true }, ...items]);
    this.term.set('');
    this.results.set([]);
    this._searched = false;
    this.open.set(false);
  }

  private moveActive(index: number): void {
    this.activeIndex.set(index);
    if (index >= 0) {
      document.getElementById(this.optionId(index))?.scrollIntoView({ block: 'nearest' });
    }
  }

  // --- selected contacts ---

  protected remove(key: number): void {
    this._selected.update(items => items.filter(item => item.key !== key));
    this._input().nativeElement.focus();
  }

  protected setEditing(key: number, editing: boolean): void {
    this._selected.update(items => items.map(item => item.key === key ? { ...item, editing } : item));
  }

  protected patch(key: number, field: EditableField, value: string): void {
    this._selected.update(items => items.map(item => item.key === key
      ? { ...item, contact: new Contact({ ...item.contact, [field]: value || undefined }) }
      : item));
  }

  private contacts(): Contact[] {
    return this._selected().map(item => new Contact({ ...item.contact }));
  }
}
