import { ChangeDetectionStrategy, Component, output } from '@angular/core';

import { ACCENT_CHIP, FlowNodeType, NODE_KINDS } from './flow.models';

/** Left rail: the catalog of node types. Clicking one drops a node onto the canvas. */
@Component({
  selector: 'app-flow-palette',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="flex h-full w-full flex-col border-r border-base-300 bg-base-100">
      <div class="border-b border-base-300 px-4 py-3.5">
        <p class="font-mono text-[0.62rem] uppercase tracking-[0.2em] text-base-content/45">
          Nodes
        </p>
        <p class="mt-0.5 text-xs text-base-content/45">Click to add to canvas</p>
      </div>

      <div class="dex-scroll flex-1 space-y-1.5 overflow-y-auto p-2.5">
        @for (kind of kinds; track kind.type) {
          <button
            type="button"
            class="group flex w-full items-start gap-3 rounded-field border border-base-200
                   bg-base-100 p-2.5 text-left transition-all hover:border-base-300
                   hover:bg-base-200 hover:shadow-sm"
            (click)="add.emit(kind.type)"
          >
            <span
              class="grid size-9 shrink-0 place-items-center rounded-field {{ chip(kind.accent) }}"
            >
              <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
                <path
                  [attr.d]="kind.icon"
                  stroke="currentColor"
                  stroke-width="1.7"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            </span>
            <span class="min-w-0 flex-1">
              <span class="block text-sm font-semibold text-base-content">{{ kind.label }}</span>
              <span class="mt-0.5 block text-xs leading-snug text-base-content/50">
                {{ kind.description }}
              </span>
            </span>
            <svg
              viewBox="0 0 24 24"
              fill="none"
              class="size-4 shrink-0 text-base-content/25 transition group-hover:text-primary"
              aria-hidden="true"
            >
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
          </button>
        }
      </div>
    </aside>
  `,
})
export class FlowPaletteComponent {
  readonly add = output<FlowNodeType>();

  protected readonly kinds = NODE_KINDS;
  protected readonly chip = (accent: keyof typeof ACCENT_CHIP) => ACCENT_CHIP[accent];
}
