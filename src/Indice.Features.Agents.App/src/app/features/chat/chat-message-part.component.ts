import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MarkdownModule } from 'ngx-markdown';

import { IChatMessagePart } from '../../core/services/dex-api.service';
import { MULTIPLE_CHOICE_MEDIA_TYPE, parseMultipleChoice } from './chat.models';
import { ChatOptionsComponent } from './parts/chat-options.component';

/**
 * Renders one content part of an assistant message according to its `contentType`. This is the single dispatch point
 * for alternative media types: a new renderable type is one more `@case` here plus its component.
 *
 * An unknown content type renders nothing — the same forward-compat discipline `chat-stream.service.ts` applies to
 * unknown SSE frame types, so a newer server can send parts this client has never heard of without breaking it.
 */
@Component({
  selector: 'app-chat-message-part',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MarkdownModule, ChatOptionsComponent],
  template: `
    @switch (part().contentType) {
      @case (multipleChoiceMediaType) {
        <app-chat-options
          [options]="options()"
          [disabled]="!interactive()"
          (pick)="optionPick.emit($event)"
        />
      }
      @case ('text/markdown') {
        <div class="markdown" markdown [data]="part().value"></div>
      }
      @case ('text') {
        <div class="markdown" markdown [data]="part().value"></div>
      }
    }
  `,
})
export class ChatMessagePartComponent {
  /** The content part to render. */
  readonly part = input.required<IChatMessagePart>();
  /** Whether interactive parts in this message may still be acted on — false for anything but the latest message. */
  readonly interactive = input(false);

  /** Emits an option the user picked, to be sent as the next user message. */
  readonly optionPick = output<string>();

  protected readonly multipleChoiceMediaType = MULTIPLE_CHOICE_MEDIA_TYPE;

  protected readonly options = computed(() => parseMultipleChoice(this.part().value));
}
