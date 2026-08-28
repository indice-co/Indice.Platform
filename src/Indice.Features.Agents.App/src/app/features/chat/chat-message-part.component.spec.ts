import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMarkdown } from 'ngx-markdown';

import { ChatMessagePartComponent } from './chat-message-part.component';
import {
  CALLOUT_MEDIA_TYPE,
  CONFIRM_MEDIA_TYPE,
  IMAGE_MEDIA_TYPE,
  MULTIPLE_CHOICE_MEDIA_TYPE,
} from './parts/part-contracts';

const CHOICE_PART = {
  contentType: MULTIPLE_CHOICE_MEDIA_TYPE,
  value: '{"options":["Tell me about faq","Tell me about identity"]}',
};

const CONFIRM_PART = {
  contentType: CONFIRM_MEDIA_TYPE,
  value: '{"prompt":"Look it up?","confirmText":"Yes, go ahead","cancelText":"No thanks"}',
};

const IMAGE_PART = {
  contentType: IMAGE_MEDIA_TYPE,
  value: '{"uri":"https://cdn.example.com/a.png","caption":"Figure 1"}',
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

  function render(
    part: { contentType?: string; value?: string; name?: string },
    options: { interactive?: boolean; first?: boolean } = {},
  ): HTMLElement {
    fixture.componentRef.setInput('part', part);
    fixture.componentRef.setInput('interactive', options.interactive ?? true);
    fixture.componentRef.setInput('first', options.first ?? false);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function buttons(host: HTMLElement): HTMLButtonElement[] {
    return Array.from(host.querySelectorAll('button'));
  }

  function bubbleOf(host: HTMLElement): DOMTokenList | undefined {
    return host.querySelector('.markdown')?.classList;
  }

  describe('prose', () => {
    it('renders a markdown part through the markdown renderer', async () => {
      // ngx-markdown renders asynchronously, so the text only lands once the fixture settles.
      const host = render({ contentType: 'text/markdown', value: 'Hello **world**' });
      expect(host.querySelector('.markdown')).toBeTruthy();
      await fixture.whenStable();
      expect(host.querySelector('.markdown')?.innerHTML).toContain('<strong>world</strong>');
    });

    it('carries its own bubble chrome, since the thread no longer wraps the parts', () => {
      const bubble = bubbleOf(render({ contentType: 'text/markdown', value: 'hi' }));
      expect(bubble).toContain('rounded-box');
      expect(bubble).toContain('border-base-300');
    });

    it('gets the bubble tail only when it is the first part of the message', () => {
      const first = bubbleOf(render({ contentType: 'text/markdown', value: 'hi' }, { first: true }));
      expect(first).toContain('rounded-tl-sm');
      const later = bubbleOf(render({ contentType: 'text/markdown', value: 'hi' }, { first: false }));
      expect(later).not.toContain('rounded-tl-sm');
    });
  });

  describe('image', () => {
    it('renders the envelope as a captioned figure', () => {
      const host = render(IMAGE_PART);
      const image = host.querySelector('img');
      expect(image?.getAttribute('src')).toBe('https://cdn.example.com/a.png');
      expect(host.querySelector('figcaption')?.textContent?.trim()).toBe('Figure 1');
    });

    it('uses the caption as the alt text as well', () => {
      // One string does both jobs: the visible caption and the image's text alternative.
      expect(render(IMAGE_PART).querySelector('img')?.getAttribute('alt')).toBe('Figure 1');
    });

    it('renders a raw image/* part whose value is the uri itself', () => {
      const host = render({ contentType: 'image/png', value: 'data:image/png;base64,AAAA' });
      expect(host.querySelector('img')?.getAttribute('src')).toBe('data:image/png;base64,AAAA');
      expect(host.querySelector('figcaption')).toBeNull();
      // No caption anywhere to describe it, so it is decorative rather than unlabelled.
      expect(host.querySelector('img')?.getAttribute('alt')).toBe('');
    });

    it('captions a raw image/* part from the part name', () => {
      // The bare shape has no payload to hold a caption; the part name is what makes it a peer of the envelope.
      const host = render({
        contentType: 'image/png',
        value: 'data:image/png;base64,AAAA',
        name: 'The same mark, carried as a bare image/png part.',
      });
      expect(host.querySelector('figcaption')?.textContent?.trim()).toBe(
        'The same mark, carried as a bare image/png part.',
      );
      expect(host.querySelector('img')?.getAttribute('alt')).toBe('The same mark, carried as a bare image/png part.');
    });

    it('renders nothing once the browser reports the image failed to load', () => {
      const host = render(IMAGE_PART);
      host.querySelector('img')!.dispatchEvent(new Event('error'));
      fixture.detectChanges();
      expect(host.querySelector('img')).toBeNull();
      expect(host.querySelector('figure')).toBeNull();
    });

    it('renders nothing for a uri whose scheme is not allowed', () => {
      const host = render({ contentType: IMAGE_MEDIA_TYPE, value: '{"uri":"javascript:alert(1)"}' });
      expect(host.querySelector('img')).toBeNull();
    });
  });

  describe('callout', () => {
    it('renders an alert with the class for its severity', () => {
      const value = '{"severity":"warning","title":"Careful","text":"Body"}';
      const alert = render({ contentType: CALLOUT_MEDIA_TYPE, value }).querySelector('.alert');
      expect(alert?.classList).toContain('alert-warning');
      expect(alert?.textContent).toContain('Careful');
      expect(alert?.textContent).toContain('Body');
    });

    it('falls back to the info style for a severity it does not know', () => {
      const value = '{"severity":"catastrophic","text":"Body"}';
      const alert = render({ contentType: CALLOUT_MEDIA_TYPE, value }).querySelector('.alert');
      expect(alert?.classList).toContain('alert-info');
    });

    it('renders nothing when the callout has no body', () => {
      const host = render({ contentType: CALLOUT_MEDIA_TYPE, value: '{"severity":"info"}' });
      expect(host.querySelector('.alert')).toBeNull();
    });
  });

  describe('multiple choice', () => {
    it('renders one button per option', () => {
      const labels = buttons(render(CHOICE_PART)).map((button) => button.textContent?.trim());
      expect(labels).toEqual(['Tell me about faq', 'Tell me about identity']);
    });

    it('emits the picked option so the page can send it as a user message', () => {
      const picked: string[] = [];
      fixture.componentInstance.pick.subscribe((option) => picked.push(option));
      buttons(render(CHOICE_PART))[1].click();
      expect(picked).toEqual(['Tell me about identity']);
    });

    it('locks the list after the first pick so a double-click cannot send twice', () => {
      const picked: string[] = [];
      fixture.componentInstance.pick.subscribe((option) => picked.push(option));
      const host = render(CHOICE_PART);
      buttons(host)[0].click();
      fixture.detectChanges();
      buttons(host)[0].click();
      expect(picked).toEqual(['Tell me about faq']);
      expect(buttons(host).every((button) => button.disabled)).toBeTrue();
    });

    it('disables the options of a message that is no longer the latest', () => {
      const host = render(CHOICE_PART, { interactive: false });
      expect(buttons(host).length).toBe(2);
      expect(buttons(host).every((button) => button.disabled)).toBeTrue();
    });

    it('renders nothing when the payload is malformed', () => {
      const host = render({ contentType: MULTIPLE_CHOICE_MEDIA_TYPE, value: 'not json' });
      expect(buttons(host).length).toBe(0);
    });
  });

  describe('confirmation', () => {
    it('renders the prompt and both labelled buttons', () => {
      const host = render(CONFIRM_PART);
      expect(host.textContent).toContain('Look it up?');
      expect(buttons(host).map((button) => button.textContent?.trim())).toEqual([
        'Yes, go ahead',
        'No thanks',
      ]);
    });

    it('emits the label of whichever button was pressed', () => {
      const picked: string[] = [];
      fixture.componentInstance.pick.subscribe((option) => picked.push(option));
      buttons(render(CONFIRM_PART))[1].click();
      expect(picked).toEqual(['No thanks']);
    });

    it('locks after the first pick so the second button cannot also fire', () => {
      const picked: string[] = [];
      fixture.componentInstance.pick.subscribe((option) => picked.push(option));
      const host = render(CONFIRM_PART);
      buttons(host)[0].click();
      fixture.detectChanges();
      buttons(host)[1].click();
      expect(picked).toEqual(['Yes, go ahead']);
    });

    it('is disabled once the message is no longer the latest', () => {
      const host = render(CONFIRM_PART, { interactive: false });
      expect(buttons(host).every((button) => button.disabled)).toBeTrue();
    });
  });

  it('renders nothing at all for an unknown content type', () => {
    const host = render({ contentType: 'application/vnd.indice.not-invented-yet+json', value: '{"a":1}' });
    expect(host.textContent?.trim()).toBe('');
    expect(host.children.length).toBe(0);
  });
});
