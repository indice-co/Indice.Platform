import { DexChatPatchOp } from './dex-api.service';
import { ChatStreamDeltaFrame } from './chat-stream.service';
import { JsonPointerPatch } from './json-pointer-patch';

/** Port of the C# JsonPointerPatchTests — the applier is the executable spec of the patch protocol. */
describe('JsonPointerPatch', () => {
  function apply(...frames: Omit<ChatStreamDeltaFrame, 'type'>[]): Record<string, any> {
    const document: Record<string, any> = {};
    const applier = new JsonPointerPatch(); // one instance per stream — carries the compaction state
    for (const frame of frames) {
      applier.apply(document, { type: 'delta', ...frame });
    }
    return document;
  }

  it('add creates a root member', () => {
    const doc = apply({ op: DexChatPatchOp.Add, path: '/limitReached', value: false });
    expect(doc['limitReached']).toBe(false);
  });

  it('add replaces an existing member', () => {
    const doc = apply(
      { op: DexChatPatchOp.Add, path: '/modelId', value: 'a' },
      { op: DexChatPatchOp.Add, path: '/modelId', value: 'b' },
    );
    expect(doc['modelId']).toBe('b');
  });

  it('add with dash appends to an array', () => {
    const doc = apply(
      { op: DexChatPatchOp.Add, path: '/items', value: [] },
      { op: DexChatPatchOp.Add, path: '/items/-', value: 'x' },
    );
    expect(doc['items'][0]).toBe('x');
  });

  it('append concatenates strings along a deep pointer', () => {
    const doc = apply(
      { op: DexChatPatchOp.Add, path: '/messages', value: [{ content: { parts: [] } }] },
      { op: DexChatPatchOp.Add, path: '/messages/0/content/parts/-', value: { value: '', contentType: 'text/markdown' } },
      { op: DexChatPatchOp.Append, path: '/messages/0/content/parts/0/value', value: 'Hello ' },
      { op: DexChatPatchOp.Append, path: '/messages/0/content/parts/0/value', value: 'world' },
    );
    expect(doc['messages'][0].content.parts[0].value).toBe('Hello world');
  });

  it('replace requires an existing member', () => {
    expect(() => apply({ op: DexChatPatchOp.Replace, path: '/missing', value: 'x' })).toThrow();
  });

  it('omitted path and op inherit from the previous delta', () => {
    const doc = apply(
      { op: DexChatPatchOp.Add, path: '/text', value: '' },
      { op: DexChatPatchOp.Append, value: 'Hello ' }, // op changed, path inherited
      { value: 'world' }, // both inherited
    );
    expect(doc['text']).toBe('Hello world');
  });

  it('first delta without path or op throws', () => {
    expect(() => apply({ value: 'x' })).toThrow();
  });

  it('pointer unescapes rfc6901 tokens', () => {
    const doc = apply(
      { op: DexChatPatchOp.Add, path: '/a~1b', value: 1 },
      { op: DexChatPatchOp.Add, path: '/c~0d', value: 2 },
    );
    expect(doc['a/b']).toBe(1);
    expect(doc['c~d']).toBe(2);
  });
});
