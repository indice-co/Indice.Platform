import { formatFileSize, parseFileReference } from './part-contracts';

describe('parseFileReference', () => {
  const id = '3f2504e0-4f89-41d3-9a0c-0305e82c3301';

  it('reads a complete file reference', () => {
    const value = JSON.stringify({ id, fileName: 'report.html', contentType: 'text/html', length: 2048, sha256: 'ab12' });
    expect(parseFileReference(value)).toEqual({
      id,
      fileName: 'report.html',
      contentType: 'text/html',
      length: 2048,
      sha256: 'ab12',
    });
  });

  it('fills in what a producer left out', () => {
    expect(parseFileReference(JSON.stringify({ id, fileName: 'a.md' }))).toEqual({
      id,
      fileName: 'a.md',
      contentType: 'application/octet-stream',
      length: 0,
      sha256: '',
    });
  });

  it('renders nothing for a payload without an id or a name', () => {
    expect(parseFileReference(JSON.stringify({ fileName: 'a.md' }))).toBeNull();
    expect(parseFileReference(JSON.stringify({ id }))).toBeNull();
    expect(parseFileReference(JSON.stringify({ id, fileName: '   ' }))).toBeNull();
  });

  it('refuses an id that is not a GUID, because it ends up in a request path', () => {
    for (const bad of ['../../etc/passwd', '123', `${id}/../x`, `${id}?download=1`, '']) {
      expect(parseFileReference(JSON.stringify({ id: bad, fileName: 'a.md' })))
        .withContext(bad)
        .toBeNull();
    }
  });

  it('degrades to null for malformed input rather than throwing', () => {
    expect(parseFileReference('not json')).toBeNull();
    expect(parseFileReference('')).toBeNull();
    expect(parseFileReference(undefined)).toBeNull();
    expect(parseFileReference('[]')).toBeNull();
  });

  it('ignores a negative or non-numeric length', () => {
    expect(parseFileReference(JSON.stringify({ id, fileName: 'a', length: -5 }))?.length).toBe(0);
    expect(parseFileReference(JSON.stringify({ id, fileName: 'a', length: '12' }))?.length).toBe(0);
  });
});

describe('formatFileSize', () => {
  const cases: [number, string][] = [
    [0, ''],
    [-1, ''],
    [Number.NaN, ''],
    [1, '1 B'],
    [1023, '1023 B'],
    [1024, '1.0 KB'],
    [39322, '38.4 KB'],
    [1048576, '1.0 MB'],
    [5 * 1024 * 1024, '5.0 MB'],
    [150 * 1024 * 1024, '150 MB'],
    [3 * 1024 ** 3, '3.0 GB'],
  ];

  for (const [bytes, expected] of cases) {
    it(`writes ${bytes} bytes as "${expected}"`, () => {
      expect(formatFileSize(bytes)).toBe(expected);
    });
  }
});
