import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChatComposerComponent } from './chat-composer.component';
import { AgentInfo } from '../../core/services/dex-api.service';

const SPARKLES = 'M12 3c.5 3.8 2.7 6 6.5 6.5-3.8.5-6 2.7-6.5 6.5-.5-3.8-2.7-6-6.5-6.5C9.3 9 11.5 6.8 12 3z';
const BOOK = 'M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2zM22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z';
const FALLBACK = 'M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z';

function agent(name: string, description: string, icon?: string): AgentInfo {
  return new AgentInfo({ name, description, inputContentTypes: [], outputContentTypes: [], icon });
}

const AGENTS = [
  agent('auto', 'Discovers user intent and routes it', 'sparkles'),
  agent('knowledge', 'Answers from a knowledge base', 'book'),
];

describe('ChatComposerComponent flow picker', () => {
  let fixture: ComponentFixture<ChatComposerComponent>;

  async function mount(agents: AgentInfo[]): Promise<HTMLElement> {
    fixture.componentRef.setInput('agents', agents);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function glyphs(el: HTMLElement): (string | null)[] {
    return Array.from(el.querySelectorAll('.dropdown-content li > button > svg path')).map((p) =>
      p.getAttribute('d'),
    );
  }

  function triggerGlyph(el: HTMLElement): string | null {
    return el.querySelector('button[aria-label="Select mode"] svg path')!.getAttribute('d');
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatComposerComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ChatComposerComponent);
  });

  it('resolves each advertised icon token to its glyph', async () => {
    expect(glyphs(await mount(AGENTS))).toEqual([SPARKLES, BOOK]);
  });

  it('falls back for a token the client does not know', async () => {
    const el = await mount([agent('research', 'Deep multi-step research', 'telescope')]);
    expect(glyphs(el)).toEqual([FALLBACK]);
  });

  it('falls back for an agent advertising no icon at all', async () => {
    // Also the pre-regen state: the generated client drops `icon` off the wire.
    const el = await mount([agent('knowledge', 'Answers from a knowledge base')]);
    expect(glyphs(el)).toEqual([FALLBACK]);
  });

  it('follows the active flow on the trigger across a pick', async () => {
    const el = await mount(AGENTS);
    expect(triggerGlyph(el)).withContext('defaults to the first agent').toBe(SPARKLES);

    el.querySelectorAll<HTMLButtonElement>('.dropdown-content li > button')[1].click();
    await fixture.whenStable();

    expect(triggerGlyph(el)).toBe(BOOK);
    expect(el.querySelector('button[aria-label="Select mode"]')!.textContent).toContain('knowledge');
  });

  it('keeps the two-row layout: icon beside a name row and a description row', async () => {
    const el = await mount(AGENTS);
    const items = el.querySelectorAll('.dropdown-content li > button');
    expect(items.length).toBe(2);

    items.forEach((item, i) => {
      const rows = item.querySelector('span.flex-col')!;
      const [nameRow, descRow] = Array.from(rows.children);
      expect(nameRow.textContent!.trim()).toContain(AGENTS[i].name);
      expect(descRow.textContent!.trim()).toBe(AGENTS[i].description);
      // The glyph is a sibling of the text block, so it sits beside both rows.
      expect(item.querySelector(':scope > svg')).toBeTruthy();
    });
  });

  it('marks the active flow for assistive tech', async () => {
    const el = await mount(AGENTS);
    const items = el.querySelectorAll('.dropdown-content li > button');
    expect(items[0].getAttribute('aria-current')).toBe('true');
    expect(items[1].getAttribute('aria-current')).toBeNull();
  });
});
