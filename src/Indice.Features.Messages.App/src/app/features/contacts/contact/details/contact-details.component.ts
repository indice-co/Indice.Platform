import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePencil } from '@lucide/angular';
import { BoolIconComponent, ChannelIconComponent } from 'src/app/ui/badges/badges';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { contactChannels } from '../../contact-channels';
import { ContactStore } from '../contact-store.service';

/** "Basic information" tab of the contact detail page. */
@Component({
  selector: 'app-contact-details',
  imports: [DatePipe, RouterLink, TranslatePipe, BoolIconComponent, ChannelIconComponent, LucidePencil],
  template: `
    <section class="panel p-6">
      <div class="mb-5 flex items-start justify-between gap-4">
        <h2 class="text-lg font-semibold">{{ 'Contacts.Title' | translate }}</h2>
        @if (store.contact(); as contact) {
          <a class="btn btn-ghost btn-sm" [routerLink]="pane.link(['update-contact', contact.id!])">
            <svg lucidePencil class="size-4"></svg>{{ 'Contacts.Edit' | translate }}
          </a>
        }
      </div>
      @if (store.contact(); as contact) {
        <dl class="grid grid-cols-1 gap-x-6 gap-y-5 sm:grid-cols-2">
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.Salutation' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.salutation || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.FullName' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.fullName || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.FirstName' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.firstName || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.LastName' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.lastName || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.Email' | translate }}</dt>
            <dd class="mt-1 font-medium break-all">{{ contact.email || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.Phone' | translate }}</dt>
            <dd class="mt-1 font-medium tabular">{{ contact.phoneNumber || '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.ContactCode' | translate }}</dt>
            <dd class="mt-1 font-medium">
              @if (contact.recipientId) {
                <span class="inline-flex items-center gap-1.5" [attr.title]="(contact.resolved ? 'Contacts.Resolved' : 'Contacts.Unknown') | translate">
                  <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ contact.recipientId }}</code>
                  <app-bool-icon [value]="contact.resolved" />
                </span>
              } @else {
                —
              }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.Channels' | translate }}</dt>
            <dd class="mt-1 flex gap-1">
              @for (channel of channels(contact); track channel.kind) {
                <app-channel-icon [kind]="channel.kind" [muted]="!channel.available" />
              }
            </dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.UpdatedAt' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.updatedAt ? (contact.updatedAt | date: 'medium') : '—' }}</dd>
          </div>
          <div>
            <dt class="text-xs text-base-content/60">{{ 'Contacts.LastResolutionDate' | translate }}</dt>
            <dd class="mt-1 font-medium">{{ contact.lastResolutionDate ? (contact.lastResolutionDate | date: 'medium') : '—' }}</dd>
          </div>
        </dl>
      } @else {
        <div class="grid grid-cols-1 gap-5 sm:grid-cols-2">
          @for (row of [1, 2, 3, 4, 5, 6]; track row) {
            <div class="space-y-2"><div class="skeleton h-3 w-24"></div><div class="skeleton h-4 w-40"></div></div>
          }
        </div>
      }
    </section>
  `
})
export class ContactDetailsComponent {
  protected readonly store = inject(ContactStore);
  protected readonly pane = inject(SidePaneService);
  protected readonly channels = contactChannels;
}
