import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MarkdownModule } from 'ngx-markdown';

import { IChatMessagePart } from '../../core/services/dex-api.service';
import { ChatCalloutComponent } from './parts/chat-callout.component';
import { ChatConfirmComponent } from './parts/chat-confirm.component';
import { ChatImageComponent } from './parts/chat-image.component';
import { ChatOptionsComponent } from './parts/chat-options.component';
import { parseCallout, parseConfirmation, parseImage, parseMultipleChoice, partKind } from './parts/part-contracts';

/**
 * Renders one content part of an assistant message according to its `contentType`. This is the single dispatch point
 * for alternative media types: a new renderable type is one more `@case` here plus its component.
 *
 * Each case also owns its own chrome, which is why the host is `display: contents` — the thread stacks parts in a flex
 * column and every case's root element becomes a flex item directly. Prose keeps the bordered bubble; media and
 * interactive parts render bare, so an image is not framed twice and option buttons are not boxed inside a card.
 *
 * A part that renders nothing — an unknown content type, or a payload that failed to parse — contributes no element at
 * all, so it costs no gap in the stack. That is the same forward-compat discipline `chat-stream.service.ts` applies to
 * unknown SSE frame types: a newer server can send parts this client has never heard of without breaking it.
 */
@Component({
  selector: 'app-chat-message-part',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  imports: [MarkdownModule, ChatCalloutComponent, ChatConfirmComponent, ChatImageComponent, ChatOptionsComponent],
  template: `
    @switch (kind()) {
      @case ('markdown') {
        <div
          class="markdown rounded-box border border-base-300 bg-base-100 px-4 py-2.5 text-[0.95rem]
                 text-base-content shadow-sm"
          [class.rounded-tl-sm]="first()"
          [class.dex-caret]="caret()"
          markdown
          [data]="part().value"
        ></div>
      }
      @case ('image') {
        <app-chat-image [image]="image()" />
      }
      @case ('multiple-choice') {
        <app-chat-options [options]="options()" [disabled]="!interactive()" (pick)="pick.emit($event)" />
      }
      @case ('callout') {
        <app-chat-callout [callout]="callout()" />
      }
      @case ('confirm') {
        <app-chat-confirm [confirmation]="confirmation()" [disabled]="!interactive()" (pick)="pick.emit($event)" />
      }
    }
  `,
})
export class ChatMessagePartComponent {
  /** The content part to render. */
  readonly part = input.required<IChatMessagePart>();
  /** Whether interactive parts in this message may still be acted on — false for anything but the latest message. */
  readonly interactive = input(false);
  /** Whether this is the message's first part; only that one gets the bubble tail pointing at the avatar. */
  readonly first = input(false);
  /** Whether to show the blinking streaming caret. Honoured only by prose — a caret on an image means nothing. */
  readonly caret = input(false);

  /** Emits text the user picked from an interactive part, to be sent as the next user message. */
  readonly pick = output<string>();

  protected readonly kind = computed(() => partKind(this.part().contentType));

  protected readonly options = computed(() => parseMultipleChoice(this.part().value));
  protected readonly image = computed(() => parseImage(this.part().value, this.part().contentType));
  protected readonly callout = computed(() => parseCallout(this.part().value));
  protected readonly confirmation = computed(() => parseConfirmation(this.part().value));
}
