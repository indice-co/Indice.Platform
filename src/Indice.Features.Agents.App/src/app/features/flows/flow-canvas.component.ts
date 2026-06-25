import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';

import {
  ACCENT_CHIP,
  FlowEdge,
  FlowNode,
  NODE_HEIGHT,
  NODE_WIDTH,
  PendingEdge,
  nodeKind,
} from './flow.models';

/**
 * The interactive flow surface: absolutely-positioned node cards over an SVG edge layer.
 * Nodes drag (pointer capture); dragging from a node's output port to another node creates an edge.
 */
@Component({
  selector: 'app-flow-canvas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="dex-scroll dex-canvas relative h-full w-full overflow-auto"
      (pointerdown)="onBackgroundPointerDown($event)"
    >
      <div #surface class="relative" [style.width.px]="surfaceWidth()" [style.height.px]="surfaceHeight()">
        <!-- Edges -->
        <svg
          class="pointer-events-none absolute inset-0 overflow-visible"
          [attr.width]="surfaceWidth()"
          [attr.height]="surfaceHeight()"
        >
          @for (edge of edges(); track edge.id) {
            <g class="group">
              <path
                [attr.d]="edgePath(edge)"
                fill="none"
                stroke-width="2"
                class="stroke-base-300 transition-colors group-hover:stroke-error/70"
              />
              <path
                [attr.d]="edgePath(edge)"
                fill="none"
                stroke="transparent"
                stroke-width="16"
                pointer-events="stroke"
                class="pointer-events-auto cursor-pointer"
                (click)="removeEdge.emit(edge.id)"
              >
                <title>Click to remove connection</title>
              </path>
            </g>
          }
          @if (pending(); as p) {
            <path
              [attr.d]="pendingPath(p)"
              fill="none"
              stroke-width="2"
              stroke-dasharray="5 4"
              class="stroke-primary"
            />
          }
        </svg>

        <!-- Nodes -->
        @for (node of nodes(); track node.id) {
          <div
            class="dex-rise absolute select-none rounded-box border bg-base-100 shadow-sm transition-shadow"
            [class.border-base-300]="node.id !== selectedId()"
            [class.border-primary]="node.id === selectedId()"
            [class.ring-2]="node.id === selectedId()"
            [class.ring-primary]="node.id === selectedId()"
            [class.dex-node-active]="node.id === activeId()"
            [class.cursor-grab]="node.id !== draggingId"
            [class.cursor-grabbing]="node.id === draggingId"
            [style.left.px]="node.x"
            [style.top.px]="node.y"
            [style.width.px]="NODE_WIDTH"
            [style.height.px]="NODE_HEIGHT"
            [attr.data-node-id]="node.id"
            (pointerdown)="onNodePointerDown($event, node)"
            (pointermove)="onNodePointerMove($event, node)"
            (pointerup)="onNodePointerUp($event, node)"
          >
            <!-- input port -->
            <span
              class="absolute -left-1.5 top-1/2 size-3 -translate-y-1/2 rounded-full border-2
                     border-base-100 bg-base-300"
              aria-hidden="true"
            ></span>

            <div class="flex h-full items-center gap-2.5 px-3">
              <span class="grid size-9 shrink-0 place-items-center rounded-field {{ chip(node) }}">
                <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                  <path
                    [attr.d]="icon(node)"
                    stroke="currentColor"
                    stroke-width="1.7"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  />
                </svg>
              </span>
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-semibold text-base-content">{{ node.title }}</p>
                <p class="truncate font-mono text-[0.62rem] uppercase tracking-[0.14em] text-base-content/45">
                  {{ kindLabel(node) }}
                </p>
              </div>
            </div>

            <!-- output port (drag to connect) -->
            <span
              class="absolute -right-1.5 top-1/2 size-3.5 -translate-y-1/2 cursor-crosshair rounded-full
                     border-2 border-base-100 bg-base-content/30 transition-colors hover:bg-primary"
              [class.bg-primary]="node.id === pending()?.from"
              title="Drag to another node to connect"
              (pointerdown)="onPortPointerDown($event, node)"
              (pointermove)="onPortPointerMove($event)"
              (pointerup)="onPortPointerUp($event)"
            ></span>
          </div>
        }
      </div>
    </div>
  `,
})
export class FlowCanvasComponent {
  readonly nodes = input<FlowNode[]>([]);
  readonly edges = input<FlowEdge[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly activeId = input<string | null>(null);
  readonly pending = input<PendingEdge | null>(null);

  readonly select = output<string>();
  readonly backgroundClick = output<void>();
  readonly move = output<{ id: string; x: number; y: number }>();
  readonly connect = output<{ from: string; to: string }>();
  readonly removeEdge = output<string>();
  readonly pendingChange = output<PendingEdge | null>();

  protected readonly NODE_WIDTH = NODE_WIDTH;
  protected readonly NODE_HEIGHT = NODE_HEIGHT;

  private readonly surface = viewChild.required<ElementRef<HTMLElement>>('surface');

  // Transient drag state (drives emitted events, not the view directly).
  protected draggingId: string | null = null;
  private dragDx = 0;
  private dragDy = 0;
  private connectFrom: string | null = null;

  private readonly nodeMap = computed(() => new Map(this.nodes().map((n) => [n.id, n])));

  protected readonly surfaceWidth = computed(() => {
    const maxX = this.nodes().reduce((m, n) => Math.max(m, n.x + NODE_WIDTH), 0);
    return Math.max(1040, maxX + 240);
  });
  protected readonly surfaceHeight = computed(() => {
    const maxY = this.nodes().reduce((m, n) => Math.max(m, n.y + NODE_HEIGHT), 0);
    return Math.max(560, maxY + 220);
  });

  protected chip(node: FlowNode): string {
    return ACCENT_CHIP[nodeKind(node.type).accent];
  }
  protected icon(node: FlowNode): string {
    return nodeKind(node.type).icon;
  }
  protected kindLabel(node: FlowNode): string {
    return nodeKind(node.type).label;
  }

  // ── Node dragging ─────────────────────────────────────────────────────────
  protected onNodePointerDown(event: PointerEvent, node: FlowNode): void {
    this.select.emit(node.id);
    const p = this.toLocal(event);
    this.draggingId = node.id;
    this.dragDx = p.x - node.x;
    this.dragDy = p.y - node.y;
    (event.currentTarget as Element).setPointerCapture(event.pointerId);
  }

  protected onNodePointerMove(event: PointerEvent, node: FlowNode): void {
    if (this.draggingId !== node.id) {
      return;
    }
    const p = this.toLocal(event);
    this.move.emit({
      id: node.id,
      x: Math.max(0, Math.round(p.x - this.dragDx)),
      y: Math.max(0, Math.round(p.y - this.dragDy)),
    });
  }

  protected onNodePointerUp(event: PointerEvent, node: FlowNode): void {
    if (this.draggingId === node.id) {
      this.draggingId = null;
      (event.currentTarget as Element).releasePointerCapture?.(event.pointerId);
    }
  }

  // ── Drag-to-connect ───────────────────────────────────────────────────────
  protected onPortPointerDown(event: PointerEvent, node: FlowNode): void {
    event.stopPropagation();
    event.preventDefault();
    this.connectFrom = node.id;
    (event.target as Element).setPointerCapture(event.pointerId);
    const p = this.toLocal(event);
    this.pendingChange.emit({ from: node.id, x: p.x, y: p.y });
  }

  protected onPortPointerMove(event: PointerEvent): void {
    if (!this.connectFrom) {
      return;
    }
    const p = this.toLocal(event);
    this.pendingChange.emit({ from: this.connectFrom, x: p.x, y: p.y });
  }

  protected onPortPointerUp(event: PointerEvent): void {
    if (!this.connectFrom) {
      return;
    }
    // Pointer capture keeps events here, so hit-test the element actually under the cursor.
    const under = document.elementFromPoint(event.clientX, event.clientY);
    const target = (under?.closest('[data-node-id]') as HTMLElement | null)?.dataset['nodeId'] ?? null;
    if (target && target !== this.connectFrom) {
      this.connect.emit({ from: this.connectFrom, to: target });
    }
    this.connectFrom = null;
    this.pendingChange.emit(null);
    (event.target as Element).releasePointerCapture?.(event.pointerId);
  }

  // ── Background deselect ───────────────────────────────────────────────────
  protected onBackgroundPointerDown(event: PointerEvent): void {
    if ((event.target as HTMLElement).closest('[data-node-id]')) {
      return; // a node (or its port) handled this
    }
    this.backgroundClick.emit();
  }

  // ── Geometry ──────────────────────────────────────────────────────────────
  protected edgePath(edge: FlowEdge): string {
    const a = this.nodeMap().get(edge.from);
    const b = this.nodeMap().get(edge.to);
    if (!a || !b) {
      return '';
    }
    return this.bezier(a.x + NODE_WIDTH, a.y + NODE_HEIGHT / 2, b.x, b.y + NODE_HEIGHT / 2);
  }

  protected pendingPath(p: PendingEdge): string {
    const a = this.nodeMap().get(p.from);
    if (!a) {
      return '';
    }
    return this.bezier(a.x + NODE_WIDTH, a.y + NODE_HEIGHT / 2, p.x, p.y);
  }

  private bezier(x1: number, y1: number, x2: number, y2: number): string {
    const dx = Math.max(36, Math.abs(x2 - x1) * 0.5);
    return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`;
  }

  private toLocal(event: PointerEvent): { x: number; y: number } {
    const rect = this.surface().nativeElement.getBoundingClientRect();
    return { x: event.clientX - rect.left, y: event.clientY - rect.top };
  }
}
