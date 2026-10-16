import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { TailwindControl } from './tailwind-control';

/** ajsf `wysiwyg` widget (Quill editor). */
@Component({
    selector: 'app-jf-wysiwyg',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <div class="mt-1 editor">
            <quill-editor
              [id]="id"
              [ngModel]="data"
              (ngModelChange)="setValue($event)"></quill-editor>
          </div>
        } @else {
          <div class="mt-1 ql-readonly">
            <quill-editor
              [ngModel]="data"
              [disabled]="true"
              [modules]="{ toolbar: false }"></quill-editor>
          </div>
        }
      </app-jf-field>
    }`,
    // Same rules as the ajsf widget (::ng-deep), written without @apply.
    styles: [`
    app-jf-wysiwyg .ql-container { min-height: 100px; }
    app-jf-wysiwyg .ql-toolbar.ql-snow { border-top-left-radius: 0.5rem; border-top-right-radius: 0.5rem; }
    app-jf-wysiwyg .ql-container.ql-snow { border-bottom-left-radius: 0.5rem; border-bottom-right-radius: 0.5rem; }
    app-jf-wysiwyg .ql-readonly .ql-container.ql-snow { border-top-left-radius: 0.5rem; border-top-right-radius: 0.5rem; }`],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class WysiwygControlComponent extends TailwindControl {
    protected widget = 'wysiwyg';
}
