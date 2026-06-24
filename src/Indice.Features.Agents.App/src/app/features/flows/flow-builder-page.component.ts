import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';

import { FlowCanvasComponent } from './flow-canvas.component';
import { FlowInspectorComponent } from './flow-inspector.component';
import { FlowPaletteComponent } from './flow-palette.component';
import {
  ConfigValue,
  FlowEdge,
  FlowNode,
  FlowNodeType,
  PendingEdge,
  createSampleFlow,
  nextId,
  nodeKind,
} from './flow.models';

/**
 * Flow Builder — a client-side, sample/placeholder visual editor for composing a RAG flow. There is
 * no backend yet; all state lives in signals here. Demonstrates palette → canvas → inspector editing
 * plus a simulated run that lights each node in topological order.
 */
@Component({
  selector: 'app-flow-builder-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlowPaletteComponent, FlowCanvasComponent, FlowInspectorComponent],
  templateUrl: './flow-builder-page.component.html',
})
export class FlowBuilderPageComponent {
  private readonly destroyRef = inject(DestroyRef);

  protected readonly nodes = signal<FlowNode[]>([]);
  protected readonly edges = signal<FlowEdge[]>([]);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly pending = signal<PendingEdge | null>(null);
  protected readonly running = signal(false);
  protected readonly activeId = signal<string | null>(null);

  protected readonly selectedNode = computed(
    () => this.nodes().find((n) => n.id === this.selectedId()) ?? null,
  );

  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.resetSample();
    this.destroyRef.onDestroy(() => this.clearTimer());
  }

  // ── Palette / structure ───────────────────────────────────────────────────
  protected addNode(type: FlowNodeType): void {
    const count = this.nodes().length;
    const node: FlowNode = {
      id: nextId('node'),
      type,
      title: nodeKind(type).label,
      // Cascade new nodes so they don't stack exactly on top of one another.
      x: 140 + (count % 5) * 34,
      y: 300 + (count % 5) * 34,
      config: { ...nodeKind(type).defaultConfig },
    };
    this.nodes.update((list) => [...list, node]);
    this.selectedId.set(node.id);
  }

  protected removeNode(id: string): void {
    this.nodes.update((list) => list.filter((n) => n.id !== id));
    this.edges.update((list) => list.filter((e) => e.from !== id && e.to !== id));
    if (this.selectedId() === id) {
      this.selectedId.set(null);
    }
  }

  protected selectNode(id: string): void {
    this.selectedId.set(id);
  }

  protected deselect(): void {
    this.selectedId.set(null);
  }

  protected moveNode(change: { id: string; x: number; y: number }): void {
    this.nodes.update((list) =>
      list.map((n) => (n.id === change.id ? { ...n, x: change.x, y: change.y } : n)),
    );
  }

  protected connect(link: { from: string; to: string }): void {
    if (link.from === link.to) {
      return;
    }
    const duplicate = this.edges().some((e) => e.from === link.from && e.to === link.to);
    if (duplicate) {
      return;
    }
    this.edges.update((list) => [...list, { id: nextId('edge'), from: link.from, to: link.to }]);
  }

  protected removeEdge(id: string): void {
    this.edges.update((list) => list.filter((e) => e.id !== id));
  }

  // ── Inspector edits ───────────────────────────────────────────────────────
  protected updateTitle(title: string): void {
    const id = this.selectedId();
    if (!id) {
      return;
    }
    this.nodes.update((list) => list.map((n) => (n.id === id ? { ...n, title } : n)));
  }

  protected updateConfig(change: { key: string; value: ConfigValue }): void {
    const id = this.selectedId();
    if (!id) {
      return;
    }
    this.nodes.update((list) =>
      list.map((n) =>
        n.id === id ? { ...n, config: { ...n.config, [change.key]: change.value } } : n,
      ),
    );
  }

  // ── Toolbar ───────────────────────────────────────────────────────────────
  protected resetSample(): void {
    this.clearTimer();
    this.running.set(false);
    this.activeId.set(null);
    const sample = createSampleFlow();
    this.nodes.set(sample.nodes);
    this.edges.set(sample.edges);
    this.selectedId.set(null);
    this.pending.set(null);
  }

  /** Toggle: light each node in topological order on a timer (signal writes drive zoneless CD). */
  protected simulateRun(): void {
    if (this.running()) {
      this.stopRun();
      return;
    }
    const order = this.runOrder();
    if (order.length === 0) {
      return;
    }
    this.running.set(true);
    this.selectedId.set(null);

    let i = 0;
    const step = (): void => {
      if (i >= order.length) {
        this.stopRun();
        return;
      }
      this.activeId.set(order[i]);
      i += 1;
      this.timer = setTimeout(step, 720);
    };
    step();
  }

  private stopRun(): void {
    this.clearTimer();
    this.activeId.set(null);
    this.running.set(false);
  }

  private clearTimer(): void {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = undefined;
    }
  }

  /** Kahn topological order; leftovers (cycles/disconnected) appended in insertion order. */
  private runOrder(): string[] {
    const nodes = this.nodes();
    const edges = this.edges();
    const indeg = new Map<string, number>(nodes.map((n) => [n.id, 0]));
    const adj = new Map<string, string[]>();
    for (const e of edges) {
      indeg.set(e.to, (indeg.get(e.to) ?? 0) + 1);
      adj.set(e.from, [...(adj.get(e.from) ?? []), e.to]);
    }
    const queue = nodes.filter((n) => (indeg.get(n.id) ?? 0) === 0).map((n) => n.id);
    const order: string[] = [];
    while (queue.length) {
      const id = queue.shift()!;
      order.push(id);
      for (const next of adj.get(id) ?? []) {
        indeg.set(next, (indeg.get(next) ?? 0) - 1);
        if ((indeg.get(next) ?? 0) === 0) {
          queue.push(next);
        }
      }
    }
    for (const n of nodes) {
      if (!order.includes(n.id)) {
        order.push(n.id);
      }
    }
    return order;
  }
}
