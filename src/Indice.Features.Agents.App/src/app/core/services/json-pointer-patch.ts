import { DexChatPatchOp } from './dex-api.service';
import { ChatStreamDeltaFrame } from './chat-stream.service';

/**
 * Applier for the Dex streaming patch protocol: RFC 6901 pointer resolution, ops add/append/replace,
 * and frame-compaction inflation (an omitted `path`/`op` inherits the previous delta's effective
 * value — one instance per stream). Port of the C# reference applier
 * (`Indice.Features.Agents.Core.Tests/Streaming/JsonPointerPatch.cs`).
 */
export class JsonPointerPatch {
  private path?: string;
  private op?: DexChatPatchOp;

  apply(document: Record<string, any>, frame: ChatStreamDeltaFrame): void {
    // `?? ` only — the empty string is the legal RFC 6901 root pointer, not inheritance.
    this.path = frame.path ?? this.path;
    this.op = frame.op ?? this.op;
    if (this.path === undefined) {
      throw new Error('First delta frame carries no path.');
    }
    if (this.op === undefined) {
      throw new Error('First delta frame carries no op.');
    }
    const segments = this.path.replace(/^\//, '').split('/').map(unescape);
    const parent = resolve(document, segments.slice(0, -1));
    const last = segments[segments.length - 1];
    switch (this.op) {
      case DexChatPatchOp.Add:
        if (Array.isArray(parent)) {
          if (last === '-') {
            parent.push(frame.value);
          } else {
            parent.splice(Number(last), 0, frame.value);
          }
        } else {
          parent[last] = frame.value;
        }
        break;
      case DexChatPatchOp.Append:
        parent[last] = ((parent[last] as string | undefined) ?? '') + (frame.value as string);
        break;
      case DexChatPatchOp.Replace:
        if (!(last in parent)) {
          throw new Error(`replace target '${this.path}' does not exist.`);
        }
        parent[last] = frame.value;
        break;
      default:
        throw new Error(`Unsupported op '${this.op}'.`);
    }
  }
}

function resolve(root: Record<string, any>, segments: string[]): any {
  let node: any = root;
  for (const segment of segments) {
    node = Array.isArray(node) ? node[Number(segment)] : node[segment];
  }
  return node;
}

function unescape(segment: string): string {
  return segment.replace(/~1/g, '/').replace(/~0/g, '~');
}
