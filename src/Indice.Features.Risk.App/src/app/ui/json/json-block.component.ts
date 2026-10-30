import { JsonPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';

/** Read-only, pretty printed JSON. Empty values render a muted placeholder. */
@Component({
  selector: 'app-json-block',
  imports: [JsonPipe],
  template: `
    @if (isEmpty()) {
      <p class="rounded-field border border-dashed border-base-300 px-4 py-3 text-sm text-base-content/50">No data</p>
    } @else {
      <pre class="max-h-[28rem] overflow-auto rounded-field border border-base-300 bg-base-200 p-4 font-mono text-xs leading-relaxed">{{ value() | json }}</pre>
    }
  `
})
export class JsonBlockComponent {
  public readonly value = input<unknown>();
  protected readonly isEmpty = computed(() => {
    const value = this.value();
    return value === null || value === undefined || (typeof value === 'object' && Object.keys(value as object).length === 0);
  });
}
