import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideInfo } from '@lucide/angular';
import { ContactChannelOption, ContactPreference, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { BoolIconComponent, CHANNEL_KINDS, ChannelIconComponent, ChannelKind, channelLabel } from 'src/app/ui/badges/badges';
import { ContactStore } from '../contact-store.service';

/** "Communication settings" tab: the contact's language, consent and channel preferences per message type (read only). */
@Component({
  selector: 'app-contact-preferences',
  imports: [DatePipe, NgTemplateOutlet, TranslatePipe, BoolIconComponent, ChannelIconComponent, LucideInfo],
  template: `
    @if (loading()) {
      <div class="space-y-4">
        <div class="skeleton h-28 w-full"></div>
        <div class="skeleton h-56 w-full"></div>
      </div>
    } @else if (preference(); as preference) {
      <div class="space-y-6">
        <section class="panel p-6">
          <h2 class="mb-5 text-lg font-semibold">{{ 'Contacts.CommunicationDetailsHeader' | translate }}</h2>
          <dl class="grid grid-cols-1 gap-x-6 gap-y-5 sm:grid-cols-3">
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Contacts.CommunicationLanguageLabel' | translate }}</dt>
              <dd class="mt-1 font-medium">
                @if (preference.locale) {
                  <span class="badge badge-sm badge-ghost font-mono uppercase">{{ preference.locale }}</span>
                } @else {
                  {{ 'Contacts.EmptyValueIndicator' | translate }}
                }
              </dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Contacts.CommunicationConsentLabel' | translate }}</dt>
              <dd class="mt-1 flex items-center gap-2 font-medium">
                <app-bool-icon [value]="preference.consentCommercial" />
                {{ (preference.consentCommercial ? 'Common.Yes' : 'Common.No') | translate }}
              </dd>
            </div>
            <div>
              <dt class="text-xs text-base-content/60">{{ 'Contacts.CommunicationConsentDateLabel' | translate }}</dt>
              <dd class="mt-1 font-medium">
                {{ preference.consentCommercialDate ? (preference.consentCommercialDate | date: 'medium') : ('Contacts.EmptyValueIndicator' | translate) }}
              </dd>
            </div>
          </dl>
        </section>

        <section class="panel overflow-hidden">
          <div class="flex flex-col gap-1 border-b border-base-300 px-6 py-4">
            <h2 class="text-lg font-semibold">{{ 'Contacts.ChannelPreferencesTitle' | translate }}</h2>
            <p class="flex items-center gap-1.5 text-sm text-base-content/60">
              <svg lucideInfo class="size-4 shrink-0"></svg>{{ 'Contacts.ChannelPreferencesInfo' | translate }}
            </p>
          </div>
          <div class="overflow-x-auto">
            <table class="table">
              <thead>
                <tr>
                  <th>{{ 'Contacts.MessageTypesColumn' | translate }}</th>
                  @for (kind of channelKinds; track kind) {
                    <th class="text-center"><app-channel-icon [kind]="kind" [label]="true" /></th>
                  }
                </tr>
              </thead>
              <tbody>
                @if (preference.defaultChannels) {
                  <tr class="bg-base-200/50">
                    <td class="font-semibold">{{ 'Contacts.DefaultSettingsRowLabel' | translate }}</td>
                    @for (kind of channelKinds; track kind) {
                      <td class="text-center">
                        <ng-container *ngTemplateOutlet="cell; context: { checked: receives(preference.defaultChannels, kind), row: ('Contacts.DefaultSettingsRowLabel' | translate), kind }" />
                      </td>
                    }
                  </tr>
                }
                @for (option of preference.communication ?? []; track $index) {
                  <tr class="hover:bg-base-200/60">
                    <td>
                      <span class="inline-flex items-center gap-2" [attr.data-class]="option.messageType?.classification" [attr.title]="option.messageType?.classification">
                        <span class="class-dot"></span>{{ option.messageType?.name || ('Contacts.EmptyValueIndicator' | translate) }}
                      </span>
                    </td>
                    @for (kind of channelKinds; track kind) {
                      <td class="text-center">
                        <ng-container *ngTemplateOutlet="cell; context: { checked: receives(option.channels, kind), row: option.messageType?.name, kind }" />
                      </td>
                    }
                  </tr>
                } @empty {
                  @if (!preference.defaultChannels) {
                    <tr><td [attr.colspan]="channelKinds.length + 1" class="py-8 text-center text-sm text-base-content/60">{{ 'Contacts.NoPreferences' | translate }}</td></tr>
                  }
                }
              </tbody>
            </table>
          </div>
        </section>
      </div>
    } @else {
      <div class="panel p-6 text-sm text-base-content/60">{{ 'Contacts.NoPreferences' | translate }}</div>
    }

    <ng-template #cell let-checked="checked" let-row="row" let-kind="kind">
      <input type="checkbox" class="checkbox checkbox-sm checkbox-primary cursor-default" [checked]="checked" aria-readonly="true" tabindex="-1"
        [attr.aria-label]="row + ' · ' + (label(kind) | translate)" (click)="$event.preventDefault()" />
    </ng-template>
  `
})
export class ContactPreferencesComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _store = inject(ContactStore);
  protected readonly channelKinds = CHANNEL_KINDS;
  protected readonly label = channelLabel;
  protected readonly loading = signal(true);
  protected readonly preference = signal<ContactPreference | undefined>(undefined);

  public ngOnInit(): void {
    this._api.getPreferences(this._store.contactId()).subscribe({
      next: preference => {
        this.preference.set(preference);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  /** Inbox messages are always delivered; other channels follow the contact's choices. */
  protected receives(options: ContactChannelOption[] | undefined, kind: ChannelKind): boolean {
    return kind === 'Inbox' || !!options?.some(option => (option.kind as string) === kind && option.include);
  }
}
