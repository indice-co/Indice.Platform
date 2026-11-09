import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QuillEditorComponent } from 'ngx-quill';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

/** ajsf `wysiwyg` widget (Quill editor; toolbar configured by `provideQuillConfig` in app.config). */
@Component({
    selector: 'app-jf-wysiwyg',
    imports: [FieldWrapperComponent, FormsModule, QuillEditorComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <quill-editor
            class="block"
            [id]="id"
            [ngModel]="data"
            (ngModelChange)="setValue($event)"></quill-editor>
        } @else {
          <div class="jf-wysiwyg-readonly">
            <quill-editor
              class="block"
              [ngModel]="data"
              [disabled]="true"
              [modules]="{ toolbar: false }"></quill-editor>
          </div>
        }
      </app-jf-field>
    }`,
    // Quill renders its own markup, so the theme is applied without view encapsulation.
    styles: [`
    app-jf-wysiwyg .ql-toolbar.ql-snow,
    app-jf-wysiwyg .ql-container.ql-snow { border-color: var(--color-base-300); font-family: inherit; }
    app-jf-wysiwyg .ql-toolbar.ql-snow { border-top-left-radius: var(--radius-field); border-top-right-radius: var(--radius-field); background: var(--color-base-200); }
    app-jf-wysiwyg .ql-container.ql-snow { min-height: 100px; border-bottom-left-radius: var(--radius-field); border-bottom-right-radius: var(--radius-field); background: var(--color-base-100); font-size: 0.875rem; }
    app-jf-wysiwyg .ql-editor { color: var(--color-base-content); }
    app-jf-wysiwyg .ql-snow .ql-stroke { stroke: var(--color-base-content); }
    app-jf-wysiwyg .ql-snow .ql-fill { fill: var(--color-base-content); }
    app-jf-wysiwyg .ql-snow .ql-picker { color: var(--color-base-content); }
    app-jf-wysiwyg .jf-wysiwyg-readonly .ql-container.ql-snow { border-radius: var(--radius-field); background: color-mix(in oklab, var(--color-base-200) 50%, transparent); }`],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class WysiwygControlComponent extends TailwindControl {
    protected widget = 'wysiwyg';
    override readonlyAsText = false;
}
