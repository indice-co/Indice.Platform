import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JsonFormsModule } from '@jsonforms/angular';
import { TranslateModule } from '@ngx-translate/core';
import { QuillModule } from 'ngx-quill';
import { CheckboxControlComponent } from './controls/checkbox.control';
import { CurrencyControlComponent } from './controls/currency.control';
import { DateControlComponent } from './controls/date.control';
import { FieldWrapperComponent } from './controls/field-wrapper.component';
import { FileControlComponent } from './controls/file.control';
import { HrefControlComponent } from './controls/href.control';
import { InputControlComponent } from './controls/input.control';
import { LookupSelectorControlComponent } from './controls/lookup-selector.control';
import { LookupControlComponent } from './controls/lookup.control';
import { SelectControlComponent } from './controls/select.control';
import { TextareaControlComponent } from './controls/textarea.control';
import { WysiwygControlComponent } from './controls/wysiwyg.control';
import { HtmlRendererComponent } from './elements/html.renderer';
import { LabelOnlyRendererComponent } from './elements/label-only.renderer';
import { SubmitRendererComponent } from './elements/submit.renderer';
import { ArrayRendererComponent } from './layouts/array.renderer';
import { CategorizationRendererComponent } from './layouts/categorization.renderer';
import { LayoutRendererComponent } from './layouts/layout.renderer';

/** The Tailwind JSON Forms renderers (see `tailwindRenderers`). Exports `JsonFormsModule` for `<jsonforms>`. */
@NgModule({
    declarations: [
        FieldWrapperComponent,
        // controls
        CheckboxControlComponent,
        CurrencyControlComponent,
        DateControlComponent,
        FileControlComponent,
        HrefControlComponent,
        InputControlComponent,
        LookupControlComponent,
        LookupSelectorControlComponent,
        SelectControlComponent,
        TextareaControlComponent,
        WysiwygControlComponent,
        // layouts and elements
        ArrayRendererComponent,
        CategorizationRendererComponent,
        HtmlRendererComponent,
        LabelOnlyRendererComponent,
        LayoutRendererComponent,
        SubmitRendererComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        JsonFormsModule,
        QuillModule,
        TranslateModule
    ],
    exports: [
        JsonFormsModule
    ]
})
export class JsonFormsTailwindModule { }
