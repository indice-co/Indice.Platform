import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowDown, LucideMail, LucidePhone, LucideUsers } from '@lucide/angular';
import { Contact, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { ContactAvatarComponent, contactDisplayName } from 'src/app/shared/components/list-contact-create/contact-avatar.component';
import { AppTranslatedToaster, ToastType } from 'src/app/shared/services/app-translated-toaster';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';

/** Side pane listing the duplicates of a contact; the selected ones are merged into it. */
@Component({
  selector: 'app-contact-duplicates',
  imports: [TranslatePipe, SidePaneLayoutComponent, ContactAvatarComponent, EmptyStateComponent, LucideArrowDown, LucideMail, LucidePhone, LucideUsers],
  template: `
    <app-side-pane-layout [title]="'Contacts.DuplicatesTitle' | translate" [subtitle]="'Contacts.DuplicatesSubtitle' | translate">
      @if (loading()) {
        <p class="mb-3 flex items-center gap-2 text-sm text-base-content/60">
          <span class="loading loading-spinner loading-xs"></span>{{ 'Contacts.DuplicatesLoading' | translate }}
        </p>
        <div class="space-y-2">
          @for (row of [1, 2, 3]; track row) { <div class="skeleton h-20 w-full"></div> }
        </div>
      } @else if (duplicates().length === 0) {
        <app-empty-state [title]="'Contacts.NoDuplicatesFound' | translate">
          <svg icon lucideUsers class="size-5"></svg>
        </app-empty-state>
      } @else {
        <div class="mb-3 flex items-center justify-between gap-2">
          <label class="flex cursor-pointer items-center gap-2 text-sm font-medium">
            <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" [checked]="allSelected()"
              [indeterminate]="selected().size > 0 && !allSelected()" (change)="toggleAll($any($event.target).checked)" />
            {{ 'Contacts.SelectAllDuplicates' | translate }}
          </label>
          <span class="text-sm text-base-content/60">{{ 'Contacts.SelectedCount' | translate: { count: selected().size } }}</span>
        </div>
        <ul class="space-y-2">
          @for (item of duplicates(); track item.id) {
            <li>
              <label class="flex cursor-pointer items-center gap-3 rounded-box border border-base-300 bg-base-100 p-3 transition-colors hover:border-base-content/30 has-checked:border-primary has-checked:bg-primary/5">
                <input type="checkbox" class="checkbox checkbox-sm checkbox-primary" [checked]="selected().has(item.id!)"
                  (change)="toggle(item.id!, $any($event.target).checked)" />
                <app-contact-avatar [name]="displayName(item)" />
                <span class="min-w-0 flex-1">
                  <span class="block truncate font-medium">
                    @if (item.salutation) { {{ item.salutation }} }
                    {{ displayName(item) || '—' }}
                  </span>
                  <span class="flex items-center gap-1.5 truncate text-sm text-base-content/70"><svg lucideMail class="size-3.5 shrink-0"></svg>{{ item.email || '—' }}</span>
                  <span class="flex items-center gap-1.5 truncate text-sm text-base-content/70"><svg lucidePhone class="size-3.5 shrink-0"></svg>{{ item.phoneNumber || '—' }}</span>
                </span>
              </label>
            </li>
          }
        </ul>
        @if (contact(); as main) {
          <div class="mt-4 flex flex-col items-center gap-2 text-sm text-base-content/60">
            <svg lucideArrowDown class="size-4"></svg>
            <div class="flex w-full items-center gap-3 rounded-box border border-dashed border-primary/50 bg-primary/5 p-3">
              <app-contact-avatar [name]="displayName(main)" size="sm" />
              <span class="min-w-0">
                <span class="block text-xs">{{ 'Contacts.MergeInto' | translate }}</span>
                <span class="block truncate font-medium text-base-content">{{ displayName(main) || '—' }}</span>
              </span>
            </div>
          </div>
        }
      }
      <div footer class="flex justify-end gap-2 border-t border-base-300 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ 'Contacts.CancelLabel' | translate }}</button>
        <button type="button" class="btn btn-primary" [disabled]="selected().size === 0 || saving()" (click)="merge()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          {{ 'Contacts.MergeLabel' | translate }}
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class ContactDuplicatesComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _toaster = inject(AppTranslatedToaster);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);
  protected readonly displayName = contactDisplayName;

  /** Bound from the `get-duplicates/:contactId` route. */
  public readonly contactId = input.required<string>();

  protected readonly contact = signal<Contact | undefined>(undefined);
  protected readonly duplicates = signal<Contact[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly selected = signal(new Set<string>());
  protected readonly allSelected = computed(() => this.duplicates().length > 0 && this.selected().size === this.duplicates().length);

  public ngOnInit(): void {
    this._api.getContactById(this.contactId()).subscribe(contact => this.contact.set(contact));
    this._api.getDuplicateContacts(this.contactId()).subscribe({
      next: result => {
        this.duplicates.set(result.items ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected toggle(id: string, checked: boolean): void {
    this.selected.update(current => {
      const next = new Set(current);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  protected toggleAll(checked: boolean): void {
    this.selected.set(new Set(checked ? this.duplicates().map(item => item.id!) : []));
  }

  /** Merges the selected duplicates into the main contact. */
  protected merge(): void {
    const name = this.contact()?.fullName || this.contact()?.email || '';
    this.saving.set(true);
    this._api.mergeContacts(this.contactId(), Array.from(this.selected())).subscribe({
      next: () => {
        this._toaster.show(ToastType.Success, 'Contacts.MergeDuplicatesSuccessTitle', 'Contacts.MergeDuplicatesSuccessMessage', undefined, { name });
        this._refresh.notify('contact');
        this._refresh.notify('contacts');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }
}
