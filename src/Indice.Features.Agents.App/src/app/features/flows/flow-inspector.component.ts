import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { ACCENT_CHIP, ConfigValue, FlowNode, nodeKind } from './flow.models';

/** Right rail: edit the selected node's title and type-specific config. */
@Component({
  selector: 'app-flow-inspector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (node(); as n) {
      <aside class="dex-scroll flex h-full w-full flex-col overflow-y-auto border-l border-base-300 bg-base-100">
        <!-- Header -->
        <div class="flex items-center gap-3 border-b border-base-300 px-4 py-3.5">
          <span class="grid size-9 shrink-0 place-items-center rounded-field {{ chip() }}">
            <svg viewBox="0 0 24 24" fill="none" class="size-5" aria-hidden="true">
              <path
                [attr.d]="kind().icon"
                stroke="currentColor"
                stroke-width="1.7"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </span>
          <div class="min-w-0">
            <p class="font-mono text-[0.62rem] uppercase tracking-[0.18em] text-base-content/45">
              {{ kind().label }} node
            </p>
            <p class="truncate text-sm font-semibold text-base-content">{{ n.title }}</p>
          </div>
        </div>

        <div class="flex-1 space-y-4 p-4">
          <!-- Title -->
          <label class="block">
            <span class="mb-1 block text-xs font-medium text-base-content/60">Label</span>
            <input
              type="text"
              class="w-full rounded-field border border-base-300 bg-base-100 px-3 py-2 text-sm
                     outline-none transition focus:border-primary/60"
              [value]="n.title"
              (input)="onTitle($event)"
            />
          </label>

          <!-- Type-specific fields -->
          @for (field of kind().fields; track field.key) {
            <div>
              <span class="mb-1 block text-xs font-medium text-base-content/60">{{ field.label }}</span>

              @switch (field.kind) {
                @case ('select') {
                  <select
                    class="w-full rounded-field border border-base-300 bg-base-100 px-3 py-2 text-sm
                           outline-none transition focus:border-primary/60"
                    [value]="asString(n.config[field.key])"
                    (change)="onSelect(field.key, $event)"
                  >
                    @for (opt of field.options ?? []; track opt) {
                      <option [value]="opt" [selected]="opt === asString(n.config[field.key])">
                        {{ opt }}
                      </option>
                    }
                  </select>
                }
                @case ('number') {
                  <input
                    type="number"
                    step="0.1"
                    class="w-full rounded-field border border-base-300 bg-base-100 px-3 py-2 text-sm
                           font-mono outline-none transition focus:border-primary/60"
                    [value]="asString(n.config[field.key])"
                    (input)="onNumber(field.key, $event)"
                  />
                }
                @case ('toggle') {
                  <label class="flex cursor-pointer items-center gap-2.5">
                    <input
                      type="checkbox"
                      class="toggle toggle-primary toggle-sm"
                      [checked]="asBool(n.config[field.key])"
                      (change)="onToggle(field.key, $event)"
                    />
                    <span class="text-sm text-base-content/70">
                      {{ asBool(n.config[field.key]) ? 'Enabled' : 'Disabled' }}
                    </span>
                  </label>
                }
                @default {
                  <input
                    type="text"
                    class="w-full rounded-field border border-base-300 bg-base-100 px-3 py-2 text-sm
                           outline-none transition focus:border-primary/60"
                    [value]="asString(n.config[field.key])"
                    (input)="onText(field.key, $event)"
                  />
                }
              }

              @if (field.hint) {
                <span class="mt-1 block font-mono text-[0.62rem] text-base-content/40">
                  {{ field.hint }}
                </span>
              }
            </div>
          }
        </div>

        <!-- Footer -->
        <div class="border-t border-base-300 p-3">
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-block justify-start gap-2 text-error hover:bg-error/10"
            (click)="remove.emit(n.id)"
          >
            <svg viewBox="0 0 24 24" fill="none" class="size-4" aria-hidden="true">
              <path
                d="M5 7h14M10 11v6M14 11v6M6 7l1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M9 7V4h6v3"
                stroke="currentColor"
                stroke-width="1.7"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
            Delete node
          </button>
        </div>
      </aside>
    }
  `,
})
export class FlowInspectorComponent {
  readonly node = input<FlowNode | null>(null);

  readonly titleChange = output<string>();
  readonly configChange = output<{ key: string; value: ConfigValue }>();
  readonly remove = output<string>();

  protected readonly kind = computed(() => nodeKind(this.node()?.type ?? 'trigger'));
  protected readonly chip = computed(() => ACCENT_CHIP[this.kind().accent]);

  protected asString(value: ConfigValue | undefined): string {
    return value === undefined ? '' : String(value);
  }
  protected asBool(value: ConfigValue | undefined): boolean {
    return value === true;
  }

  protected onTitle(event: Event): void {
    this.titleChange.emit((event.target as HTMLInputElement).value);
  }
  protected onSelect(key: string, event: Event): void {
    this.configChange.emit({ key, value: (event.target as HTMLSelectElement).value });
  }
  protected onText(key: string, event: Event): void {
    this.configChange.emit({ key, value: (event.target as HTMLInputElement).value });
  }
  protected onNumber(key: string, event: Event): void {
    const n = (event.target as HTMLInputElement).valueAsNumber;
    this.configChange.emit({ key, value: Number.isNaN(n) ? 0 : n });
  }
  protected onToggle(key: string, event: Event): void {
    this.configChange.emit({ key, value: (event.target as HTMLInputElement).checked });
  }
}
