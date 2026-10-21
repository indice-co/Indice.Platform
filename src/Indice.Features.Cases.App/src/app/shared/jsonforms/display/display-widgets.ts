import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePaperclip } from '@lucide/angular';
import { FileControlComponent } from '../controls/file.control';
import { LookupSelectorControlComponent } from '../controls/lookup-selector.control';
import { LookupControlComponent } from '../controls/lookup.control';
import { DisplayFieldComponent } from './display-field.component';

// The ajsf widgets whose value needs their edit control's logic to be shown (lookups, attachments) keep that logic
// and only swap the template for the read-only view's label / value pair. None of them shows a required marker.

/** `lookup` widget, read-only: the "value - name" of the stored value. */
@Component({
    selector: 'app-jf-display-lookup',
    imports: [DisplayFieldComponent],
    template: `
    @if (shown) {
      <app-jf-display-field [label]="notitle ? null : title" [text]="readonlyText" />
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayLookupControlComponent extends LookupControlComponent {
    override showLabel = false;
}

/** `lookup-selector` widget, read-only: the lookup name of the stored value. */
@Component({
    selector: 'app-jf-display-lookup-selector',
    imports: [DisplayFieldComponent],
    template: `
    @if (shown) {
      <app-jf-display-field [label]="notitle ? null : title" [text]="readonlyText" />
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayLookupSelectorControlComponent extends LookupSelectorControlComponent {
    override showLabel = false;
}

/** `file` widget, read-only: a download button for the attachment. */
@Component({
    selector: 'app-jf-display-file',
    imports: [DisplayFieldComponent, TranslatePipe, LucidePaperclip],
    template: `
    @if (shown) {
      <app-jf-display-field [label]="notitle ? null : title" [custom]="true">
        @if (isGuid(data)) {
          <button type="button" class="btn btn-sm btn-soft -ml-0.5 mt-0.5" (click)="onDownload()">
            <svg lucidePaperclip class="size-4"></svg>
            {{ 'common.download' | translate }}
          </button>
        } @else {
          <span class="text-base-content/40">{{ 'common.noDocumentSubmitted' | translate }}</span>
        }
      </app-jf-display-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayFileControlComponent extends FileControlComponent {
    override showLabel = false;
}
