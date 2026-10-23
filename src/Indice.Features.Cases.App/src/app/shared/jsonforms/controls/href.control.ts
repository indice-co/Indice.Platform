import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideExternalLink } from '@lucide/angular';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** ajsf `href` widget: a text input, shown as a link when read-only. */
@Component({
    selector: 'app-jf-href',
    imports: [FieldWrapperComponent, LucideExternalLink],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <input
            type="text"
            [class]="fieldClass"
            [attr.maxlength]="options.maxLength"
            [attr.minlength]="options.minLength"
            [attr.pattern]="options.pattern"
            [attr.placeholder]="options.placeholder"
            [attr.required]="options.required"
            [id]="id"
            [value]="data ?? ''"
            (input)="setValue($any($event.target).value)">
        } @else if (data) {
          <p class="min-h-10 rounded-field border border-base-300 bg-base-200/50 px-3 py-2 text-sm">
            <a [href]="data" class="link link-primary inline-flex items-center gap-1 break-all" target="_blank" rel="noopener">
              {{ data }}<svg lucideExternalLink class="size-3.5 shrink-0"></svg>
            </a>
          </p>
        } @else {
          <p class="min-h-10 rounded-field border border-base-300 bg-base-200/50 px-3 py-2 text-sm text-base-content/50">—</p>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class HrefControlComponent extends TailwindControl {
    protected widget = 'href';
    override readonlyAsText = false;
}
