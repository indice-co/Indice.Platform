import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A label / value pair of the read-only case view. Without projected content it shows `text`, or a dash when that is empty.
 * Eager change detection: the display controls' fields are mutated by JSON Forms outside of signals.
 */
@Component({
    selector: 'app-jf-display-field',
    host: { class: 'block min-w-0' },
    template: `
    @if (label()) {
      <p class="text-xs font-medium text-base-content/60" [innerHTML]="label()"></p>
    }
    <div class="mt-0.5 text-sm break-words">
      @if (custom()) {
        <ng-content />
      } @else if (text()) {
        <span class="font-medium whitespace-pre-line">{{ text() }}</span>
      } @else {
        <span class="text-base-content/40">—</span>
      }
    </div>`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayFieldComponent {
    /** Label html; `null` for fields without one (`label: false`, table cells). */
    readonly label = input<string | null>(null);
    readonly text = input<string>('');
    /** Show the projected content instead of `text`. */
    readonly custom = input(false);
}
