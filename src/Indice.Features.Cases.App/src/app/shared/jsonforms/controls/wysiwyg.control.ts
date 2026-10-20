import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { QuillEditorComponent } from 'ngx-quill';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `wysiwyg` widget (Quill editor; read-only without a toolbar). Styled in styles/jsonforms.css (`.jf-wysiwyg`). */
@Component({
    selector: 'app-jf-wysiwyg',
    imports: [FieldWrapperComponent, FormsModule, QuillEditorComponent],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <div class="jf-wysiwyg editor" [class.jf-wysiwyg-invalid]="invalid">
            <quill-editor
              [id]="id"
              [ngModel]="data"
              (ngModelChange)="setValue($event)"></quill-editor>
          </div>
        } @else {
          <div class="jf-wysiwyg ql-readonly">
            <quill-editor
              [ngModel]="data"
              [disabled]="true"
              [modules]="{ toolbar: false }"></quill-editor>
          </div>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class WysiwygControlComponent extends CaseFormControl {
    protected widget = 'wysiwyg';
}
