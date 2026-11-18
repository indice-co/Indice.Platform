import { attachmentFileName, resolveApiFileUrl, saveAttachment } from './attachment-download';

describe('resolveApiFileUrl', () => {
  const origin = 'https://app.example';
  const link = '/api/campaigns/attachments/abc.pdf';

  it('keeps the domain when the api url is relative to the app', () => {
    expect(resolveApiFileUrl(link, '/api', origin)).toBe('https://app.example/api/campaigns/attachments/abc.pdf');
    expect(resolveApiFileUrl(link, '', origin)).toBe('https://app.example/api/campaigns/attachments/abc.pdf');
  });

  it('resolves against the api host without doubling the slash', () => {
    expect(resolveApiFileUrl(link, 'https://api.example/api', origin)).toBe('https://api.example/api/campaigns/attachments/abc.pdf');
    expect(resolveApiFileUrl('api/campaigns/attachments/abc.pdf', 'https://api.example/', origin)).toBe('https://api.example/api/campaigns/attachments/abc.pdf');
  });

  it('refuses links of other hosts', () => {
    expect(resolveApiFileUrl('//api/campaigns/attachments/abc.pdf', '/api', origin)).toBeUndefined();
    expect(resolveApiFileUrl('https://attacker.example/abc.pdf', 'https://api.example/api', origin)).toBeUndefined();
    expect(resolveApiFileUrl('javascript:alert(1)', '/api', origin)).toBeUndefined();
  });
});

describe('inbox attachments', () => {
  let createdBlobs: Blob[];

  beforeEach(() => {
    createdBlobs = [];
    spyOn(URL, 'createObjectURL').and.callFake((blob: Blob | MediaSource) => {
      createdBlobs.push(blob as Blob);
      return `blob:${window.location.origin}/${createdBlobs.length}`;
    });
    spyOn(URL, 'revokeObjectURL');
  });

  it('derives the file name from the url', () => {
    expect(attachmentFileName('https://api.example/api/messages/attachments/abc.pdf?x=1')).toBe('abc.pdf');
    expect(attachmentFileName('/api/messages/attachments/')).toBe('attachment');
  });

  for (const [name, type] of [['abc.png', 'image/png'], ['abc.pdf', 'application/pdf'], ['abc.html', 'text/html'], ['abc.svg', 'image/svg+xml']]) {
    it(`saves ${name} as an opaque file download`, () => {
      const open = spyOn(window, 'open');
      const clicked: HTMLAnchorElement[] = [];
      spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) { clicked.push(this); });
      saveAttachment(new Blob(['content'], { type }), name);
      expect(open).not.toHaveBeenCalled();
      expect(clicked.length).toBe(1);
      expect(clicked[0].download).toBe(name);
      expect(clicked[0].href).toBe(`blob:${window.location.origin}/1`);
      expect(clicked[0].isConnected).toBeFalse();
      expect(createdBlobs[0].type).toBe('application/octet-stream');
    });
  }
});
