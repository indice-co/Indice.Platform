import {
  CALLOUT_MEDIA_TYPE,
  CONFIRM_MEDIA_TYPE,
  IMAGE_MEDIA_TYPE,
  MULTIPLE_CHOICE_MEDIA_TYPE,
  PartKind,
  parseCallout,
  parseConfirmation,
  parseImage,
  parseMultipleChoice,
  partKind,
} from './part-contracts';

describe('partKind', () => {
  const cases: [string | undefined, PartKind][] = [
    ['text/markdown', 'markdown'],
    ['text', 'markdown'],
    [MULTIPLE_CHOICE_MEDIA_TYPE, 'multiple-choice'],
    [IMAGE_MEDIA_TYPE, 'image'],
    [CALLOUT_MEDIA_TYPE, 'callout'],
    [CONFIRM_MEDIA_TYPE, 'confirm'],
    // Prefix matching is the whole reason this is a function and not a template @switch.
    ['image/png', 'image'],
    ['image/svg+xml', 'image'],
    ['application/vnd.indice.not-invented-yet+json', 'unknown'],
    ['application/pdf', 'unknown'],
    [undefined, 'unknown'],
  ];

  for (const [contentType, expected] of cases) {
    it(`classifies ${contentType ?? 'undefined'} as ${expected}`, () => {
      expect(partKind(contentType)).toBe(expected);
    });
  }
});

describe('parseMultipleChoice', () => {
  it('reads the options out of a well-formed payload', () => {
    expect(parseMultipleChoice('{"options":["one","two"]}')).toEqual(['one', 'two']);
  });

  it('returns an empty list for a malformed payload rather than throwing', () => {
    // A renderer calls this from a template — a bad payload must degrade to "nothing to show".
    expect(parseMultipleChoice('not json')).toEqual([]);
    expect(parseMultipleChoice('')).toEqual([]);
    expect(parseMultipleChoice(undefined)).toEqual([]);
  });

  it('returns an empty list when options is missing or not an array', () => {
    expect(parseMultipleChoice('{}')).toEqual([]);
    expect(parseMultipleChoice('{"options":"one"}')).toEqual([]);
    expect(parseMultipleChoice('[]')).toEqual([]);
  });

  it('drops entries that are not usable option text', () => {
    expect(parseMultipleChoice('{"options":["one",null,42,"  ","two"]}')).toEqual(['one', 'two']);
  });
});

describe('parseImage', () => {
  it('reads url, alt and caption out of the envelope', () => {
    const payload = '{"url":"https://cdn.example.com/a.png","alt":"A","caption":"Figure 1"}';
    expect(parseImage(payload, IMAGE_MEDIA_TYPE)).toEqual({
      url: 'https://cdn.example.com/a.png',
      alt: 'A',
      caption: 'Figure 1',
    });
  });

  it('treats a raw image/* part value as the url itself', () => {
    expect(parseImage('https://cdn.example.com/a.png', 'image/png')).toEqual({ url: 'https://cdn.example.com/a.png' });
    expect(parseImage('data:image/png;base64,AAAA', 'image/png')).toEqual({ url: 'data:image/png;base64,AAAA' });
  });

  it('accepts a same-origin root-relative url, which is how the SPA offers its own assets', () => {
    expect(parseImage('{"url":"/dex-logo.png"}', IMAGE_MEDIA_TYPE)).toEqual({
      url: '/dex-logo.png',
      alt: undefined,
      caption: undefined,
    });
  });

  it('rejects any scheme other than http, https, data:image and a root-relative path', () => {
    // The payload is ultimately model-influenced, so this is refused before it ever reaches [src].
    expect(parseImage('{"url":"javascript:alert(1)"}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{"url":"data:text/html;base64,AAAA"}', IMAGE_MEDIA_TYPE)).toBeNull();
    // Protocol-relative: a leading slash that still points off-origin.
    expect(parseImage('{"url":"//evil.example.com/x.png"}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('javascript:alert(1)', 'image/png')).toBeNull();
  });

  it('returns null for a malformed or urlless payload', () => {
    expect(parseImage('not json', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{"url":42}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage(undefined, IMAGE_MEDIA_TYPE)).toBeNull();
  });

  it('drops alt and caption that are not text', () => {
    expect(parseImage('{"url":"https://a/b.png","alt":7,"caption":null}', IMAGE_MEDIA_TYPE)).toEqual({
      url: 'https://a/b.png',
      alt: undefined,
      caption: undefined,
    });
  });
});

describe('parseCallout', () => {
  it('reads severity, title and text', () => {
    expect(parseCallout('{"severity":"warning","title":"Careful","text":"Body"}')).toEqual({
      severity: 'warning',
      title: 'Careful',
      text: 'Body',
    });
  });

  it('falls back to info for a severity it does not know', () => {
    expect(parseCallout('{"severity":"catastrophic","text":"Body"}')?.severity).toBe('info');
    expect(parseCallout('{"text":"Body"}')?.severity).toBe('info');
  });

  it('returns null when there is no body to show', () => {
    // An alert with no text is an empty coloured box — better to render nothing.
    expect(parseCallout('{"severity":"info"}')).toBeNull();
    expect(parseCallout('{"severity":"info","text":"   "}')).toBeNull();
    expect(parseCallout('not json')).toBeNull();
    expect(parseCallout(undefined)).toBeNull();
  });
});

describe('parseConfirmation', () => {
  it('reads the prompt and both labels', () => {
    expect(parseConfirmation('{"prompt":"Sure?","confirmText":"Do it","cancelText":"Stop"}')).toEqual({
      prompt: 'Sure?',
      confirmText: 'Do it',
      cancelText: 'Stop',
    });
  });

  it('falls back to Yes/No when the labels are missing, matching the server defaults', () => {
    expect(parseConfirmation('{}')).toEqual({ prompt: undefined, confirmText: 'Yes', cancelText: 'No' });
    expect(parseConfirmation('{"confirmText":"  ","cancelText":9}')?.confirmText).toBe('Yes');
  });

  it('returns null for a malformed payload', () => {
    expect(parseConfirmation('not json')).toBeNull();
    expect(parseConfirmation(undefined)).toBeNull();
  });
});
