import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideArrowLeft, LucideCircleCheck, LucideCircleX, LucideClock, LucideEye, LucideX } from '@lucide/angular';
import { MessageContent, MessagesApiClient, RecipientMessageEvents } from 'src/app/core/services/messages-api.service';
import { ThemeService } from 'src/app/layout/theme.service';
import { SafePipe } from 'src/app/shared/pipes/safe.pipe';
import { ChannelIconComponent } from 'src/app/ui/badges/badges';
import { DIALOG_DATA, DialogRef, DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { CONTENT_CHANNELS, ContentChannel } from '../../../campaign-utils';

interface ContentPreviewData {
  channel: ContentChannel;
  content: MessageContent;
}

/** Modal with the content a recipient received on one channel. */
@Component({
  selector: 'app-campaign-content-preview-dialog',
  imports: [TranslatePipe, SafePipe, ChannelIconComponent, LucideX],
  template: `
    <div class="flex items-start justify-between gap-4">
      <div class="flex min-w-0 items-center gap-3">
        <app-channel-icon [kind]="data.channel.kind" />
        <h3 class="truncate text-lg font-semibold">{{ data.content.title || (data.channel.name | translate) }}</h3>
      </div>
      <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="ref.close()" [attr.aria-label]="'Common.Close' | translate">
        <svg lucideX class="size-5"></svg>
      </button>
    </div>
    <div class="mt-4">
      @if (isHtml) {
        <iframe class="h-[60vh] w-full rounded-field border border-base-300" [title]="data.content.title ?? ''" [srcdoc]="document | safe: 'html'"></iframe>
      } @else {
        <p class="rounded-field bg-base-200 p-4 text-sm whitespace-pre-wrap">{{ data.content.body }}</p>
      }
    </div>
  `
})
class CampaignContentPreviewDialogComponent {
  protected readonly data = inject<ContentPreviewData>(DIALOG_DATA);
  protected readonly ref = inject(DialogRef);
  protected readonly isHtml = this.data.channel.key === 'email' || this.data.channel.key === 'inbox';
  protected readonly document =
    `<style>:root { color-scheme: ${inject(ThemeService).isDark() ? 'dark' : 'light'}; font-family: ui-sans-serif, system-ui, sans-serif; }</style>${this.data.content.body ?? ''}`;
}

/** A recipient's contact details, the content they received and the delivery events per channel. */
@Component({
  selector: 'app-campaign-message-timeline',
  imports: [DatePipe, RouterLink, TranslatePipe, ChannelIconComponent, EmptyStateComponent, LucideArrowLeft, LucideCircleCheck, LucideCircleX, LucideClock, LucideEye],
  template: `
    @let recipient = timeline()?.recipient;
    <a class="btn btn-ghost btn-xs -ml-2 mb-4" routerLink="../../campaign-messages">
      <svg lucideArrowLeft class="size-4"></svg>{{ 'Campaigns.RecipientsTitle' | translate }}
    </a>

    @if (loading()) {
      <div class="grid gap-6 lg:grid-cols-[22rem_1fr]">
        <div class="skeleton h-80 w-full"></div>
        <div class="skeleton h-80 w-full"></div>
      </div>
    } @else {
      <div class="grid items-start gap-6 lg:grid-cols-[22rem_1fr]">
        <div class="space-y-6">
          <section class="panel p-5">
            <h2 class="mb-4 font-semibold">{{ 'Campaigns.ContactInfoHeader' | translate }}</h2>
            <dl class="grid gap-4">
              @for (field of contactFields; track field.label) {
                <div>
                  <dt class="text-xs text-base-content/60">{{ field.label | translate }}</dt>
                  <dd class="font-medium break-all">{{ $any(recipient)?.[field.key] || ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
                </div>
              }
              <div>
                <dt class="text-xs text-base-content/60">{{ 'Campaigns.UpdatedAtLabel' | translate }}</dt>
                <dd class="font-medium tabular">{{ recipient?.updatedAt ? (recipient?.updatedAt | date: 'medium') : ('Campaigns.EmptyValueIndicator' | translate) }}</dd>
              </div>
            </dl>
          </section>

          <section class="panel p-5">
            <h2 class="mb-3 font-semibold">{{ 'Campaigns.PreviewHeader' | translate }}</h2>
            <div class="grid gap-2">
              @for (item of contents(); track item.channel.key) {
                <button type="button" class="btn btn-sm justify-start" [disabled]="!item.content" (click)="preview(item.channel, item.content)">
                  <app-channel-icon [kind]="item.channel.kind" [muted]="!item.content" />
                  {{ item.channel.name | translate }}
                  @if (item.content) { <svg lucideEye class="ml-auto size-4 opacity-60"></svg> }
                </button>
              }
            </div>
          </section>
        </div>

        <section class="panel p-5 sm:p-6">
          <h2 class="mb-5 font-semibold">{{ 'Campaigns.HistoryHeader' | translate }}</h2>
          @if (timeline()?.events?.length) {
            <ul class="timeline timeline-vertical timeline-snap-icon max-md:timeline-compact">
              @for (event of timeline()?.events; track event.id ?? $index; let first = $first; let last = $last; let even = $even) {
                <li>
                  @if (!first) { <hr [class]="event.success ? 'bg-success/40' : 'bg-error/40'" /> }
                  <div class="timeline-middle">
                    @if (event.success) {
                      <svg lucideCircleCheck class="size-5 text-success"></svg>
                    } @else {
                      <svg lucideCircleX class="size-5 text-error"></svg>
                    }
                  </div>
                  <div class="mb-6" [class]="even ? 'timeline-start ml-2 md:ml-0 md:mr-3 md:text-end' : 'timeline-end ml-2 md:ml-3'">
                    <div class="flex flex-wrap items-center gap-2" [class]="even ? 'md:justify-end' : ''">
                      <app-channel-icon [kind]="event.channel" [label]="true" />
                      <span class="badge badge-sm badge-ghost">{{ event.type }}</span>
                      @if (!event.success) {
                        <span class="badge badge-sm badge-soft badge-error">{{ 'Campaigns.FailedLabel' | translate }}</span>
                      }
                    </div>
                    <time class="mt-1 flex items-center gap-1.5 text-xs text-base-content/60 tabular" [class]="even ? 'md:justify-end' : ''">
                      <svg lucideClock class="size-3.5"></svg>{{ event.createdOn | date: 'medium' }}
                    </time>
                    @if (event.recipient) {
                      <p class="mt-1 text-xs text-base-content/70 break-all">{{ event.recipient }}</p>
                    }
                  </div>
                  @if (!last) { <hr [class]="event.success ? 'bg-success/40' : 'bg-error/40'" /> }
                </li>
              }
            </ul>
          } @else {
            <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate" [message]="'Campaigns.NoEventsMessage' | translate">
              <svg icon lucideClock class="size-5"></svg>
            </app-empty-state>
          }
        </section>
      </div>
    }
  `
})
export class CampaignMessageTimelineComponent implements OnInit {
  private readonly _api = inject(MessagesApiClient);
  private readonly _route = inject(ActivatedRoute);
  private readonly _dialog = inject(DialogService);

  /** Bound from the `campaign-message-timeline/:contactId` route param. */
  public readonly contactId = input.required<string>();

  protected readonly loading = signal(true);
  protected readonly timeline = signal<RecipientMessageEvents | undefined>(undefined);
  protected readonly contactFields = [
    { label: 'Campaigns.DisplayNameLabel', key: 'fullName' },
    { label: 'Campaigns.SalutationLabel', key: 'salutation' },
    { label: 'Campaigns.FirstNameLabel', key: 'firstName' },
    { label: 'Campaigns.LastNameLabel', key: 'lastName' },
    { label: 'Campaigns.EmailLabel', key: 'email' },
    { label: 'Campaigns.PhoneLabel', key: 'phoneNumber' },
    { label: 'Campaigns.ContactCodeLabel', key: 'id' }
  ];
  protected readonly contents = computed(() => {
    const content = this.timeline()?.content ?? {};
    const keys = Object.keys(content);
    return CONTENT_CHANNELS.map(channel => {
      const key = keys.find(k => k.toLowerCase() === channel.key.toLowerCase());
      return { channel, content: key ? content[key] : undefined };
    });
  });

  public ngOnInit(): void {
    const campaignId = this._route.parent?.snapshot.paramMap.get('campaignId');
    if (!campaignId) {
      return;
    }
    this._api.getCampaignMessageDetails(campaignId, this.contactId()).subscribe({
      next: timeline => {
        this.timeline.set(timeline);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected preview(channel: ContentChannel, content: MessageContent | undefined): void {
    if (content) {
      this._dialog.open<void, ContentPreviewData>(CampaignContentPreviewDialogComponent, { data: { channel, content }, size: 'xl' });
    }
  }
}
