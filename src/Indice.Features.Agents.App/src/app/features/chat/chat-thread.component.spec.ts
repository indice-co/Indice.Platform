import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMarkdown } from 'ngx-markdown';

import { ChatMessageContent, ChatMessagePart } from '../../core/services/dex-api.service';
import { ChatThreadComponent } from './chat-thread.component';
import { ThreadMessage } from './chat.models';
import { MULTIPLE_CHOICE_MEDIA_TYPE } from './parts/part-contracts';

const CHOICE_VALUE = '{"options":["Tell me about faq"]}';

describe('ChatThreadComponent', () => {
  let fixture: ComponentFixture<ChatThreadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatThreadComponent],
      providers: [provideZonelessChangeDetection(), provideMarkdown()],
    }).compileComponents();
    fixture = TestBed.createComponent(ChatThreadComponent);
  });

  function render(messages: ThreadMessage[]): HTMLElement {
    fixture.componentRef.setInput('messages', messages);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('stacks each part as its own block instead of welding them into one bubble', () => {
    const host = render([
      assistant([
        part('text/markdown', 'Here is what I found.'),
        part(MULTIPLE_CHOICE_MEDIA_TYPE, CHOICE_VALUE),
        part('text/markdown', 'Anything else?'),
      ]),
    ]);

    const stack = host.querySelector('.flex.flex-col.gap-3');
    expect(stack).toBeTruthy();
    expect(stack!.querySelectorAll('app-chat-message-part').length).toBe(3);
    // Two prose bubbles, not one merged card — the choice part between them is what splits them.
    expect(stack!.querySelectorAll('.markdown').length).toBe(2);
  });

  it('gives the bubble tail to the first part only', () => {
    const host = render([
      assistant([part('text/markdown', 'First.'), part('text/markdown', 'Second.')]),
    ]);

    const bubbles = Array.from(host.querySelectorAll('.markdown'));
    expect(bubbles[0].classList).toContain('rounded-tl-sm');
    expect(bubbles[1].classList).not.toContain('rounded-tl-sm');
  });

  it('leaves no element in the stack for a part it cannot render', () => {
    const host = render([
      assistant([
        part('text/markdown', 'Here is what I found.'),
        part('application/vnd.indice.not-invented-yet+json', '{"a":1}'),
      ]),
    ]);

    const hosts = Array.from(host.querySelectorAll('app-chat-message-part'));
    expect(hosts.length).toBe(2);
    // The host is display:contents, so an empty one occupies no row and eats no gap.
    expect(hosts[1].children.length).toBe(0);
  });

  it('keeps interactive parts live only on the last message of the thread', () => {
    const host = render([
      assistant([part(MULTIPLE_CHOICE_MEDIA_TYPE, CHOICE_VALUE)]),
      user('Tell me about faq'),
      assistant([part(MULTIPLE_CHOICE_MEDIA_TYPE, CHOICE_VALUE)]),
    ]);

    const optionButtons = Array.from(host.querySelectorAll<HTMLButtonElement>('app-chat-options button'));
    expect(optionButtons.length).toBe(2);
    expect(optionButtons[0].disabled).toBeTrue(); // answered — it is no longer last
    expect(optionButtons[1].disabled).toBeFalse();
  });

  it('re-emits what an interactive part was picked, for the page to send', () => {
    const picked: string[] = [];
    fixture.componentInstance.pick.subscribe((text) => picked.push(text));
    const host = render([assistant([part(MULTIPLE_CHOICE_MEDIA_TYPE, CHOICE_VALUE)])]);

    host.querySelector<HTMLButtonElement>('app-chat-options button')!.click();

    expect(picked).toEqual(['Tell me about faq']);
  });
});

function part(contentType: string, value: string): ChatMessagePart {
  return new ChatMessagePart({ contentType, value });
}

function assistant(parts: ChatMessagePart[]): ThreadMessage {
  return { role: 'Assistant', content: new ChatMessageContent({ parts }) };
}

function user(text: string): ThreadMessage {
  return { role: 'User', content: new ChatMessageContent({ parts: [part('text/markdown', text)] }) };
}
