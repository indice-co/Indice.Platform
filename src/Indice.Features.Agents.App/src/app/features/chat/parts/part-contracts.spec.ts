import {
  CALLOUT_MEDIA_TYPE,
  CONFIRM_MEDIA_TYPE,
  HITL_REQUEST_MEDIA_TYPE,
  HITL_RESPONSE_MEDIA_TYPE,
  IMAGE_MEDIA_TYPE,
  MULTIPLE_CHOICE_MEDIA_TYPE,
  PartKind,
  hitlResponseParts,
  isTextPart,
  parseCallout,
  parseConfirmation,
  parseHitlRequest,
  parseImage,
  parseMultipleChoice,
  partKind,
  textParts,
} from './part-contracts';

describe('partKind', () => {
  const cases: [string | undefined, PartKind][] = [
    ['text/markdown', 'markdown'],
    ['text', 'markdown'],
    [MULTIPLE_CHOICE_MEDIA_TYPE, 'multiple-choice'],
    [IMAGE_MEDIA_TYPE, 'image'],
    [CALLOUT_MEDIA_TYPE, 'callout'],
    [CONFIRM_MEDIA_TYPE, 'confirm'],
    [HITL_REQUEST_MEDIA_TYPE, 'hitl-request'],
    // The response half is deliberately unclassified: it only ever rides a user turn, which the dispatcher never sees.
    [HITL_RESPONSE_MEDIA_TYPE, 'unknown'],
    // Prefix matching is the whole reason this is a function and not a template @switch.
    ['image/png', 'image'],
    ['image/svg+xml', 'image'],
    ['text/html', 'html'],
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
  it('reads uri and caption out of the envelope', () => {
    const payload = '{"uri":"https://cdn.example.com/a.png","caption":"Figure 1"}';
    expect(parseImage(payload, IMAGE_MEDIA_TYPE)).toEqual({
      uri: 'https://cdn.example.com/a.png',
      caption: 'Figure 1',
    });
  });

  it('still reads the legacy `url` spelling, which is what older messages carry', () => {
    // Contents are persisted verbatim, so image parts stored before the field was renamed are still on disk as `url`.
    expect(parseImage('{"url":"https://cdn.example.com/a.png","caption":"F1"}', IMAGE_MEDIA_TYPE)).toEqual({
      uri: 'https://cdn.example.com/a.png',
      caption: 'F1',
    });
  });

  it('still reads the legacy `alt` spelling as the caption', () => {
    // `alt` and `caption` were two fields until they were collapsed; parts persisted before that carry the old pair.
    expect(parseImage('{"uri":"https://a/b.png","alt":"A diagram"}', IMAGE_MEDIA_TYPE)).toEqual({
      uri: 'https://a/b.png',
      caption: 'A diagram',
    });
  });

  it('prefers `caption` over the legacy `alt` when a payload carries both', () => {
    const payload = '{"uri":"https://a/b.png","alt":"old","caption":"new"}';
    expect(parseImage(payload, IMAGE_MEDIA_TYPE)?.caption).toBe('new');
  });

  it('prefers `uri` when a payload somehow carries both', () => {
    expect(parseImage('{"uri":"https://a/new.png","url":"https://a/old.png"}', IMAGE_MEDIA_TYPE)?.uri).toBe(
      'https://a/new.png',
    );
  });

  it('treats a raw image/* part value as the uri itself', () => {
    expect(parseImage('https://cdn.example.com/a.png', 'image/png')).toEqual({
      uri: 'https://cdn.example.com/a.png',
      caption: undefined,
    });
    expect(parseImage('data:image/png;base64,AAAA', 'image/png')).toEqual({
      uri: 'data:image/png;base64,AAAA',
      caption: undefined,
    });
  });

  it('captions a raw image/* part from the part name, the only place one can live', () => {
    // Without the envelope there is no payload to hold a caption — this is what makes the bare shape a peer of it.
    expect(parseImage('data:image/png;base64,AAAA', 'image/png', 'The Dex mark')).toEqual({
      uri: 'data:image/png;base64,AAAA',
      caption: 'The Dex mark',
    });
  });

  it('falls back to the part name for an envelope that carries no caption', () => {
    expect(parseImage('{"uri":"https://a/b.png"}', IMAGE_MEDIA_TYPE, 'From the part')?.caption).toBe('From the part');
  });

  it('prefers the envelope caption over the part name', () => {
    const payload = '{"uri":"https://a/b.png","caption":"In the payload"}';
    expect(parseImage(payload, IMAGE_MEDIA_TYPE, 'On the part')?.caption).toBe('In the payload');
  });

  it('accepts a same-origin root-relative uri, which is how the SPA offers its own assets', () => {
    expect(parseImage('{"uri":"/dex-logo.png"}', IMAGE_MEDIA_TYPE)).toEqual({
      uri: '/dex-logo.png',
      caption: undefined,
    });
  });

  it('rejects any scheme other than http, https, data:image and a root-relative path', () => {
    // The payload is ultimately model-influenced, so this is refused before it ever reaches [src].
    expect(parseImage('{"uri":"javascript:alert(1)"}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{"uri":"data:text/html;base64,AAAA"}', IMAGE_MEDIA_TYPE)).toBeNull();
    // The legacy spelling gets no free pass through the scheme check.
    expect(parseImage('{"url":"javascript:alert(1)"}', IMAGE_MEDIA_TYPE)).toBeNull();
    // Protocol-relative: a leading slash that still points off-origin.
    expect(parseImage('{"uri":"//evil.example.com/x.png"}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('javascript:alert(1)', 'image/png')).toBeNull();
    // A part name does not rescue an unrenderable uri — the caption is not a way in.
    expect(parseImage('javascript:alert(1)', 'image/png', 'Harmless-looking caption')).toBeNull();
  });

  it('returns null for a malformed or uriless payload', () => {
    expect(parseImage('not json', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage('{"uri":42}', IMAGE_MEDIA_TYPE)).toBeNull();
    expect(parseImage(undefined, IMAGE_MEDIA_TYPE)).toBeNull();
  });

  it('drops a caption that is not text, and a blank part name', () => {
    expect(parseImage('{"uri":"https://a/b.png","alt":7,"caption":null}', IMAGE_MEDIA_TYPE)).toEqual({
      uri: 'https://a/b.png',
      caption: undefined,
    });
    expect(parseImage('{"uri":"https://a/b.png"}', IMAGE_MEDIA_TYPE, '   ')?.caption).toBeUndefined();
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

describe('parseHitlRequest', () => {
  it('reads the PascalCase payload the server actually emits', () => {
    // `HumanRequest` has no [JsonPropertyName] attributes and JsonPart serialises with the default options, so this
    // is the shape on the wire — the only payload here that is not camelCase.
    expect(parseHitlRequest('{"Text":null,"RequestId":"a1b2","Properties":{}}')).toEqual({
      requestId: 'a1b2',
      text: undefined,
      properties: undefined,
    });
  });

  it('reads a camelCase payload too, in case the attributes get added', () => {
    expect(parseHitlRequest('{"requestId":"a1b2","text":"VAT number?"}')).toEqual({
      requestId: 'a1b2',
      text: 'VAT number?',
      properties: undefined,
    });
  });

  it('reads the prompt and properties when the server fills them', () => {
    const payload = '{"RequestId":"a1b2","Text":"VAT number?","Properties":{"caseId":"77"}}';
    expect(parseHitlRequest(payload)).toEqual({
      requestId: 'a1b2',
      text: 'VAT number?',
      properties: { caseId: '77' },
    });
  });

  it('still reports a request when the payload carries nothing but the id', () => {
    // Unlike a bodiless callout this must not degrade to null: "is an answer owed?" and "does the form render?" are
    // the same predicate, so a promptless request would otherwise stop the next turn carrying an answer. Which is
    // the live case — the server sends only a RequestId and puts the question in sibling prose parts.
    expect(parseHitlRequest('{"RequestId":"a1b2"}')).toEqual({
      requestId: 'a1b2',
      text: undefined,
      properties: undefined,
    });
    expect(parseHitlRequest('{}')).toEqual({ requestId: undefined, text: undefined, properties: undefined });
  });

  it('drops members that are not usable text', () => {
    expect(parseHitlRequest('{"RequestId":7,"Text":"   ","Properties":{"a":1}}')).toEqual({
      requestId: undefined,
      text: undefined,
      properties: undefined,
    });
  });

  it('returns null for a malformed payload rather than throwing', () => {
    expect(parseHitlRequest('not json')).toBeNull();
    expect(parseHitlRequest('[]')).toBeNull();
    expect(parseHitlRequest('')).toBeNull();
    expect(parseHitlRequest(undefined)).toBeNull();
  });
});

describe('hitlResponseParts', () => {
  it('leads with the plain-text answer, which is the part the server validates', () => {
    // ChatRequest.Text resolves to the first text part and ChatRequestValidator requires it non-empty; and only
    // text/plain exactly is promoted to TextContent, so text/markdown here would take the DataContent branch.
    const [text] = hitlResponseParts({ requestId: 'a1b2' }, 'EL123456789');
    expect(text).toEqual({ value: 'EL123456789', contentType: 'text/plain' });
  });

  it('carries the structured answer as a data URI, not as raw json', () => {
    // DataContent's string constructor takes a data URI; raw json would not survive ChatMessagePart.ToAIContent().
    const [, structured] = hitlResponseParts({ requestId: 'a1b2' }, 'EL123456789');
    expect(structured.contentType).toBe(HITL_RESPONSE_MEDIA_TYPE);
    expect(structured.value).toContain(`data:${HITL_RESPONSE_MEDIA_TYPE};base64,`);
  });

  it('emits the PascalCase HumanResponse shape the server would deserialize', () => {
    // Default JsonSerializerOptions are case-sensitive with no naming policy, and HumanResponse carries no
    // attributes — camelCase would bind into an empty object.
    expect(decodeResponse(hitlResponseParts({ requestId: 'a1b2' }, 'EL123456789')[1].value)).toEqual({
      Text: 'EL123456789',
      RequestId: 'a1b2',
      Properties: {},
    });
  });

  it('sends an empty correlation id when the request carried none', () => {
    // RequestId is a non-nullable string server-side, so an empty one is closer than an absent member.
    expect(decodeResponse(hitlResponseParts({}, 'yes')[1].value)).toEqual({
      Text: 'yes',
      RequestId: '',
      Properties: {},
    });
  });

  it('encodes an answer outside latin-1, which bare btoa rejects', () => {
    // The UI ships Greek content, so this is the ordinary case rather than an edge one.
    const answer = 'Ναι, το ΑΦΜ είναι EL123456789 — ευχαριστώ';
    expect(decodeResponse(hitlResponseParts({ requestId: 'a1b2' }, answer)[1].value)).toEqual({
      Text: answer,
      RequestId: 'a1b2',
      Properties: {},
    });
  });
});

describe('textParts', () => {
  it('builds the single text/plain part of an ordinary turn', () => {
    expect(textParts('hello')).toEqual([{ value: 'hello', contentType: 'text/plain' }]);
  });
});

describe('isTextPart', () => {
  it('admits prose, including a part that declares no type at all', () => {
    expect(isTextPart('text/plain')).toBeTrue();
    expect(isTextPart('text/markdown')).toBeTrue();
    expect(isTextPart('TEXT/HTML')).toBeTrue();
    expect(isTextPart(undefined)).toBeTrue();
  });

  it('rejects structured payload, whose raw value must never reach a bubble', () => {
    expect(isTextPart(HITL_RESPONSE_MEDIA_TYPE)).toBeFalse();
    expect(isTextPart('image/png')).toBeFalse();
  });
});

/** Reads a response part's value back — the inverse of the encoding in `hitlResponseParts`. */
function decodeResponse(value: string | undefined): unknown {
  const base64 = (value ?? '').slice(`data:${HITL_RESPONSE_MEDIA_TYPE};base64,`.length);
  return JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(base64), (char) => char.charCodeAt(0))));
}
