import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMarkdown } from 'ngx-markdown';

import { ChatMessagePartComponent } from './chat-message-part.component';
import { MULTIPLE_CHOICE_MEDIA_TYPE } from './chat.models';

const CHOICE_PART = {
  contentType: MULTIPLE_CHOICE_MEDIA_TYPE,
  value: '{"options":["Tell me about faq","Tell me about identity"]}',
};

describe('ChatMessagePartComponent', () => {
  let fixture: ComponentFixture<ChatMessagePartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatMessagePartComponent],
      providers: [provideZonelessChangeDetection(), provideMarkdown()],
    }).compileComponents();
    fixture = TestBed.createComponent(ChatMessagePartComponent);
  });

  function render(part: { contentType?: string; value?: string }, interactive = true): HTMLElement {
    fixture.componentRef.setInput('part', part);
    fixture.componentRef.setInput('interactive', interactive);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function buttons(host: HTMLElement): HTMLButtonElement[] {
    return Array.from(host.querySelectorAll('button'));
  }

  it('renders a markdown part through the markdown renderer', async () => {
    // ngx-markdown renders asynchronously, so the text only lands once the fixture settles.
    const host = render({ contentType: 'text/markdown', value: 'Hello **world**' });
    expect(host.querySelector('.markdown')).toBeTruthy();
    await fixture.whenStable();
    expect(host.querySelector('.markdown')?.innerHTML).toContain('<strong>world</strong>');
  });

  it('renders one button per option of a multiple-choice part', () => {
    const labels = buttons(render(CHOICE_PART)).map((button) => button.textContent?.trim());
    expect(labels).toEqual(['Tell me about faq', 'Tell me about identity']);
  });

  it('emits the picked option so the page can send it as a user message', () => {
    const picked: string[] = [];
    fixture.componentInstance.optionPick.subscribe((option) => picked.push(option));
    buttons(render(CHOICE_PART))[1].click();
    expect(picked).toEqual(['Tell me about identity']);
  });

  it('locks the list after the first pick so a double-click cannot send twice', () => {
    const picked: string[] = [];
    fixture.componentInstance.optionPick.subscribe((option) => picked.push(option));
    const host = render(CHOICE_PART);
    buttons(host)[0].click();
    fixture.detectChanges();
    buttons(host)[0].click();
    expect(picked).toEqual(['Tell me about faq']);
    expect(buttons(host).every((button) => button.disabled)).toBeTrue();
  });

  it('disables the options of a message that is no longer the latest', () => {
    const host = render(CHOICE_PART, false);
    expect(buttons(host).length).toBe(2);
    expect(buttons(host).every((button) => button.disabled)).toBeTrue();
  });

  it('renders nothing for an unknown content type', () => {
    const host = render({ contentType: 'application/vnd.indice.something-new+json', value: '{"a":1}' });
    expect(host.textContent?.trim()).toBe('');
    expect(host.children.length).toBe(0);
  });

  it('renders nothing when a multiple-choice payload is malformed', () => {
    const host = render({ contentType: MULTIPLE_CHOICE_MEDIA_TYPE, value: 'not json' });
    expect(buttons(host).length).toBe(0);
  });
});
