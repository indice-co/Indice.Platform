import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowLeft, LucideGitMerge, LucideInfo, LucideMail, LucideMegaphone, LucidePencil, LucidePhone, LucideRefreshCw, LucideSlidersHorizontal } from '@lucide/angular';
import { ContactAvatarComponent, contactDisplayName } from 'src/app/shared/components/list-contact-create/contact-avatar.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { RouteTab, RouteTabsComponent } from 'src/app/ui/tabs/route-tabs.component';
import { ContactStore } from './contact-store.service';

/** Contact detail page: header with identifiers and actions, tabs for details, campaigns and preferences. */
@Component({
  selector: 'app-contact',
  imports: [
    RouterLink, RouterOutlet, TranslatePipe, RouteTabsComponent, ContactAvatarComponent,
    LucideArrowLeft, LucideGitMerge, LucideMail, LucidePencil, LucidePhone, LucideRefreshCw
  ],
  providers: [ContactStore],
  template: `
    <header class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div class="min-w-0">
        <a class="btn btn-ghost btn-xs -ml-2 mb-2" routerLink="..">
          <svg lucideArrowLeft class="size-4"></svg>{{ 'Contacts.ListTitle' | translate }}
        </a>
        @if (store.contact(); as contact) {
          <div class="flex items-center gap-4">
            <app-contact-avatar [name]="name()" size="lg" />
            <div class="min-w-0">
              <h1 class="truncate text-2xl font-bold tracking-tight sm:text-[1.75rem]">
                @if (contact.salutation) { <span class="font-normal text-base-content/60">{{ contact.salutation }}</span> }
                {{ name() || '—' }}
              </h1>
              <div class="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-base-content/70">
                @if (contact.email) {
                  <span class="inline-flex items-center gap-1.5"><svg lucideMail class="size-4"></svg>{{ contact.email }}</span>
                }
                @if (contact.phoneNumber) {
                  <span class="inline-flex items-center gap-1.5 tabular"><svg lucidePhone class="size-4"></svg>{{ contact.phoneNumber }}</span>
                }
                @if (contact.recipientId) {
                  <span class="inline-flex items-center gap-1.5">
                    <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ contact.recipientId }}</code>
                    @if (contact.resolved) {
                      <span class="badge badge-sm badge-soft badge-success">{{ 'Contacts.Resolved' | translate }}</span>
                    } @else {
                      <span class="badge badge-sm badge-ghost">{{ 'Contacts.Unknown' | translate }}</span>
                    }
                  </span>
                }
                @if (contact.isAnonymous) {
                  <span class="badge badge-sm badge-ghost">{{ 'Contacts.Anonymous' | translate }}</span>
                }
              </div>
            </div>
          </div>
        } @else {
          <div class="flex items-center gap-4">
            <div class="skeleton size-14 shrink-0 rounded-full"></div>
            <div class="space-y-2">
              <div class="skeleton h-7 w-56"></div>
              <div class="skeleton h-4 w-72"></div>
            </div>
          </div>
        }
      </div>
      @if (store.contact(); as contact) {
        <div class="flex flex-wrap items-center gap-2">
          @if (contact.recipientId) {
            <button type="button" class="btn btn-sm btn-ghost" [disabled]="resolving()" (click)="resolve(contact.recipientId)"
              [attr.title]="'Contacts.ResolveTooltip' | translate">
              @if (resolving()) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideRefreshCw class="size-4"></svg> }
              {{ 'Contacts.Resolve' | translate }}
            </button>
            <a class="btn btn-sm" [routerLink]="pane.link(['get-duplicates', contact.id!])">
              <svg lucideGitMerge class="size-4"></svg>{{ 'Contacts.Duplicates' | translate }}
            </a>
          }
          <a class="btn btn-sm btn-primary" [routerLink]="pane.link(['update-contact', contact.id!])">
            <svg lucidePencil class="size-4"></svg>{{ 'Contacts.Edit' | translate }}
          </a>
        </div>
      }
    </header>

    <app-route-tabs class="mb-6" [tabs]="tabs()" />
    <router-outlet />
  `
})
export class ContactComponent implements OnChanges {
  protected readonly store = inject(ContactStore);
  protected readonly pane = inject(SidePaneService);

  /** Bound from the `:contactId` route param. */
  public readonly contactId = input.required<string>();

  protected readonly resolving = signal(false);
  protected readonly name = computed(() => contactDisplayName(this.store.contact()));
  protected readonly tabs = computed<RouteTab[]>(() => [
    { label: 'Contacts.BasicInformation', link: 'contact-details', icon: LucideInfo },
    { label: 'Contacts.Campaigns', link: 'contact-campaigns', icon: LucideMegaphone },
    ...(this.store.contact()?.recipientId ? [{ label: 'Contacts.CommunicationSettings', link: 'contact-preferences', icon: LucideSlidersHorizontal }] : [])
  ]);

  constructor() {
    inject(RefreshBus).on('contact').pipe(takeUntilDestroyed()).subscribe(() => this.store.reload());
  }

  public ngOnChanges(): void {
    // runs before the tabs are created, so they can read the contact id from the store right away
    this.store.load(this.contactId());
  }

  protected resolve(recipientId: string): void {
    this.resolving.set(true);
    this.store.resolve(recipientId).subscribe({
      next: () => this.resolving.set(false),
      error: () => this.resolving.set(false)
    });
  }
}
