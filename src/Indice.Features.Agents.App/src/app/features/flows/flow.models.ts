/**
 * In-memory model for the Flow Builder demo. There is no backend for flows yet, so everything here
 * is sample/placeholder data the page manipulates entirely client-side.
 */

export type FlowNodeType = 'trigger' | 'retrieve' | 'filter' | 'llm' | 'answer';

export type ConfigValue = string | number | boolean;

/** A configurable field on a node, used by the inspector to render the right control generically. */
export interface ConfigField {
  key: string;
  label: string;
  kind: 'text' | 'number' | 'select' | 'toggle';
  options?: readonly string[];
  /** Hint shown under numeric/text inputs (e.g. a unit or range). */
  hint?: string;
}

/** Static metadata for a node type: palette entry + canvas styling + inspector schema. */
export interface NodeKind {
  type: FlowNodeType;
  label: string;
  description: string;
  /** SVG path `d` for a 24×24 stroked glyph. */
  icon: string;
  /** DaisyUI semantic color used as the node's accent (border/icon tint). */
  accent: 'secondary' | 'info' | 'warning' | 'primary' | 'success';
  defaultConfig: Record<string, ConfigValue>;
  fields: readonly ConfigField[];
}

/** A placed node on the canvas. `x`/`y` are the top-left pixel position within the canvas surface. */
export interface FlowNode {
  id: string;
  type: FlowNodeType;
  title: string;
  x: number;
  y: number;
  config: Record<string, ConfigValue>;
}

/** A directed connection from one node's output port to another node's input port. */
export interface FlowEdge {
  id: string;
  from: string;
  to: string;
}

/** A connection being dragged out from a node before it lands on a target. */
export interface PendingEdge {
  from: string;
  x: number;
  y: number;
}

/** Fixed card geometry — the canvas uses these to anchor ports and route edges. */
export const NODE_WIDTH = 212;
export const NODE_HEIGHT = 74;

export const NODE_KINDS: readonly NodeKind[] = [
  {
    type: 'trigger',
    label: 'Trigger',
    description: 'Where the flow starts',
    icon: 'M13 2 4.5 13.5H11l-1 8.5 8.5-11.5H12z',
    accent: 'secondary',
    defaultConfig: { source: 'User question' },
    fields: [
      { key: 'source', label: 'Source', kind: 'select', options: ['User question', 'Webhook', 'Schedule'] },
    ],
  },
  {
    type: 'retrieve',
    label: 'Retrieve',
    description: 'Pull relevant context',
    icon: 'M4 7c0 1.7 3.6 3 8 3s8-1.3 8-3-3.6-3-8-3-8 1.3-8 3zM4 7v10c0 1.7 3.6 3 8 3s8-1.3 8-3V7M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3',
    accent: 'info',
    defaultConfig: { index: 'faq', topK: 5 },
    fields: [
      { key: 'index', label: 'Knowledge index', kind: 'select', options: ['faq', 'docs', 'all'] },
      { key: 'topK', label: 'Top K chunks', kind: 'number', hint: '1–20' },
    ],
  },
  {
    type: 'filter',
    label: 'Filter',
    description: 'Drop low-relevance hits',
    icon: 'M3 5h18l-7 8v6l-4-2v-4z',
    accent: 'warning',
    defaultConfig: { minScore: 0.7 },
    fields: [{ key: 'minScore', label: 'Min. score', kind: 'number', hint: '0.0–1.0' }],
  },
  {
    type: 'llm',
    label: 'Generate',
    description: 'Compose a grounded answer',
    icon: 'M12 3l1.9 4.6L18.5 9.5 13.9 11.4 12 16l-1.9-4.6L5.5 9.5l4.6-1.9zM18 15l.8 2 2 .8-2 .8-.8 2-.8-2-2-.8 2-.8z',
    accent: 'primary',
    defaultConfig: { model: 'claude-opus-4-8', temperature: 0.2 },
    fields: [
      {
        key: 'model',
        label: 'Model',
        kind: 'select',
        options: ['claude-opus-4-8', 'claude-sonnet-4-6', 'claude-haiku-4-5'],
      },
      { key: 'temperature', label: 'Temperature', kind: 'number', hint: '0.0–1.0' },
    ],
  },
  {
    type: 'answer',
    label: 'Answer',
    description: 'Return reply + citations',
    icon: 'M9 17l-5-5 5-5M4 12h11a5 5 0 0 1 5 5v2',
    accent: 'success',
    defaultConfig: { citations: true, tone: 'Concise' },
    fields: [
      { key: 'citations', label: 'Include citations', kind: 'toggle' },
      { key: 'tone', label: 'Tone', kind: 'select', options: ['Concise', 'Detailed', 'Friendly'] },
    ],
  },
];

/**
 * Accent → explicit Tailwind classes. Written as literals (not interpolated) so the Tailwind v4
 * scanner picks them up from this source file.
 */
export const ACCENT_CHIP: Record<NodeKind['accent'], string> = {
  secondary: 'bg-secondary/10 text-secondary',
  info: 'bg-info/10 text-info',
  warning: 'bg-warning/15 text-warning',
  primary: 'bg-primary/10 text-primary',
  success: 'bg-success/10 text-success',
};

export const ACCENT_BAR: Record<NodeKind['accent'], string> = {
  secondary: 'bg-secondary',
  info: 'bg-info',
  warning: 'bg-warning',
  primary: 'bg-primary',
  success: 'bg-success',
};

const KIND_BY_TYPE = new Map<FlowNodeType, NodeKind>(NODE_KINDS.map((k) => [k.type, k]));

export function nodeKind(type: FlowNodeType): NodeKind {
  // Every FlowNodeType has a catalog entry, so this is always defined.
  return KIND_BY_TYPE.get(type)!;
}

let idSeq = 0;
/** Monotonic, collision-free id for nodes/edges (keeps counting across resets). */
export function nextId(prefix: string): string {
  idSeq += 1;
  return `${prefix}-${idSeq}`;
}

/** A ready-made RAG flow: Trigger → Retrieve → Generate → Answer. */
export function createSampleFlow(): { nodes: FlowNode[]; edges: FlowEdge[] } {
  const make = (type: FlowNodeType, x: number, y: number): FlowNode => ({
    id: nextId('node'),
    type,
    title: nodeKind(type).label,
    x,
    y,
    config: { ...nodeKind(type).defaultConfig },
  });

  const trigger = make('trigger', 48, 96);
  const retrieve = make('retrieve', 312, 96);
  const generate = make('llm', 576, 96);
  const answer = make('answer', 840, 96);

  return {
    nodes: [trigger, retrieve, generate, answer],
    edges: [
      { id: nextId('edge'), from: trigger.id, to: retrieve.id },
      { id: nextId('edge'), from: retrieve.id, to: generate.id },
      { id: nextId('edge'), from: generate.id, to: answer.id },
    ],
  };
}
