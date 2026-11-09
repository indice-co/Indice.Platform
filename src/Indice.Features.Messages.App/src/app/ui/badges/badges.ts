import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideBell, LucideCircleCheck, LucideCircleMinus, LucideInbox, LucideMail, LucideMessageSquareText } from '@lucide/angular';

/** The channel kinds known to the UI (string values of the API's `MessageChannelKind`). */
export type ChannelKind = 'Inbox' | 'PushNotification' | 'Email' | 'SMS';

export const CHANNEL_KINDS: ChannelKind[] = ['Email', 'SMS', 'PushNotification', 'Inbox'];

/** CSS color (theme variable) for a channel kind. */
export function channelColor(kind: string | undefined): string {
  switch (kind) {
    case 'Email': return 'var(--app-channel-email)';
    case 'SMS': return 'var(--app-channel-sms)';
    case 'PushNotification': return 'var(--app-channel-push)';
    case 'Inbox': return 'var(--app-channel-inbox)';
    default: return 'var(--color-base-content)';
  }
}

/** Translation key for a channel kind's display name. */
export function channelLabel(kind: string | undefined): string {
  return `Channels.${kind ?? 'None'}`;
}

/**
 * Icon for a communication channel, tinted with the channel color.
 * `muted` greys it out (e.g. a channel a campaign does not use); `label` also prints the channel name.
 */
@Component({
  selector: 'app-channel-icon',
  imports: [TranslatePipe, LucideMail, LucideMessageSquareText, LucideBell, LucideInbox],
  host: { class: 'inline-flex items-center gap-1.5 align-middle', '[attr.title]': 'title()' },
  template: `
    <span class="grid size-6 shrink-0 place-items-center rounded-md" [style.color]="muted() ? null : color()"
      [style.background]="muted() ? null : 'color-mix(in oklab, ' + color() + ' 14%, transparent)'"
      [class]="muted() ? 'bg-base-200 text-base-content/25' : ''">
      @switch (kind()) {
        @case ('Email') { <svg lucideMail class="size-3.5"></svg> }
        @case ('SMS') { <svg lucideMessageSquareText class="size-3.5"></svg> }
        @case ('PushNotification') { <svg lucideBell class="size-3.5"></svg> }
        @default { <svg lucideInbox class="size-3.5"></svg> }
      }
    </span>
    @if (label()) {
      <span class="text-sm">{{ labelKey() | translate }}</span>
    }
  `
})
export class ChannelIconComponent {
  public readonly kind = input.required<string | undefined>();
  public readonly muted = input(false);
  public readonly label = input(false);
  protected readonly color = computed(() => channelColor(this.kind()));
  protected readonly labelKey = computed(() => channelLabel(this.kind()));
  protected readonly title = computed(() => this.kind());
}

/** Message type pill with its classification color dot. */
@Component({
  selector: 'app-message-type-badge',
  host: { class: 'inline-flex' },
  template: `
    @if (name()) {
      <span class="badge badge-sm gap-1.5 border-base-300 bg-base-100 font-medium whitespace-nowrap" [attr.data-class]="classification()"
        [attr.title]="classification()">
        <span class="class-dot"></span>{{ name() }}
      </span>
    } @else {
      <span class="text-base-content/40">—</span>
    }
  `
})
export class MessageTypeBadgeComponent {
  public readonly name = input<string | undefined>();
  public readonly classification = input<string | undefined>();
}

/** Yes / no indicator for boolean table cells. */
@Component({
  selector: 'app-bool-icon',
  imports: [TranslatePipe, LucideCircleCheck, LucideCircleMinus],
  host: { class: 'inline-flex align-middle' },
  template: `
    @if (value()) {
      <svg lucideCircleCheck class="size-5 text-success" [attr.aria-label]="'Common.Yes' | translate"></svg>
    } @else {
      <svg lucideCircleMinus class="size-5 text-base-content/30" [attr.aria-label]="'Common.No' | translate"></svg>
    }
  `
})
export class BoolIconComponent {
  public readonly value = input<boolean | null | undefined>(false);
}
