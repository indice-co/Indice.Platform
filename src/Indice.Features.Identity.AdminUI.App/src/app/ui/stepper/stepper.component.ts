import { Component, input, output } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

/**
 * Wizard progress header. The host owns the current index and renders the active step's content itself
 * (usually with `@switch`). Completed steps are clickable so users can go back.
 */
@Component({
  selector: 'app-stepper',
  imports: [LucideCheck],
  host: { class: 'block' },
  template: `
    <ol class="flex flex-wrap items-center gap-x-2 gap-y-3">
      @for (step of steps(); track $index; let i = $index; let last = $last) {
        <li class="flex items-center gap-2">
          <button type="button" class="flex items-center gap-2.5 rounded-field px-1 py-0.5 text-left"
            [disabled]="i >= current()" [class.cursor-default]="i >= current()" (click)="select.emit(i)"
            [attr.aria-current]="i === current() ? 'step' : null">
            <span class="grid size-7 shrink-0 place-items-center rounded-full text-xs font-bold tabular transition-colors"
              [class]="i < current() ? 'bg-primary text-primary-content' : i === current() ? 'bg-primary/15 text-primary ring-2 ring-primary' : 'bg-base-200 text-base-content/50'">
              @if (i < current()) { <svg lucideCheck class="size-4"></svg> } @else { {{ i + 1 }} }
            </span>
            <span class="text-sm font-semibold whitespace-nowrap" [class.text-base-content/50]="i > current()">{{ step }}</span>
          </button>
          @if (!last) {
            <span class="hidden h-px w-8 sm:block" [class]="i < current() ? 'bg-primary' : 'bg-base-300'"></span>
          }
        </li>
      }
    </ol>
  `
})
export class StepperComponent {
  public readonly steps = input.required<string[]>();
  public readonly current = input.required<number>();
  /** Emits the index of a completed step the user clicked. */
  public readonly select = output<number>();
}
