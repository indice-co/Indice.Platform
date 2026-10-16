import { and, isBooleanControl, isControl, isDateControl, isEnumControl, isIntegerControl, isNumberControl, isObjectArrayControl, isOneOfEnumControl, isPrimitiveArrayControl, isStringControl, JsonFormsRendererRegistryEntry, or, rankWith, Tester, uiTypeIs } from '@jsonforms/core';
import { CheckboxControlComponent } from './controls/checkbox.control';
import { CurrencyControlComponent } from './controls/currency.control';
import { DateControlComponent } from './controls/date.control';
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

/** A control whose layout item had one of these ajsf widget types (`options.widget`). */
const widgetIs = (...widgets: string[]): Tester => and(isControl, uischema => widgets.includes(uischema.options?.['widget']));
const hasTitleMap: Tester = and(isControl, uischema => !!uischema.options?.['titleMap']);

/**
 * The Tailwind renderer set for `<jsonforms [renderers]>`. A control with an ajsf widget type (`options.widget`) gets that widget (rank 10);
 * otherwise the schema decides, as in ajsf: enum / titleMap -> select, boolean -> checkbox, `format: date` -> date, string / number -> input.
 */
export const tailwindRenderers: JsonFormsRendererRegistryEntry[] = [
    // Layouts and elements.
    { tester: rankWith(1, or(uiTypeIs('VerticalLayout'), uiTypeIs('HorizontalLayout'), uiTypeIs('Group'), uiTypeIs('Category'))), renderer: LayoutRendererComponent },
    { tester: rankWith(1, uiTypeIs('Categorization')), renderer: CategorizationRendererComponent },
    { tester: rankWith(1, uiTypeIs('Html')), renderer: HtmlRendererComponent },
    { tester: rankWith(1, uiTypeIs('Submit')), renderer: SubmitRendererComponent },
    { tester: rankWith(10, or(uiTypeIs('LabelOnly'), widgetIs('label-only'))), renderer: LabelOnlyRendererComponent },
    // Controls chosen by the schema.
    { tester: rankWith(1, or(isStringControl, isNumberControl, isIntegerControl)), renderer: InputControlComponent },
    { tester: rankWith(2, isBooleanControl), renderer: CheckboxControlComponent },
    { tester: rankWith(2, isDateControl), renderer: DateControlComponent },
    { tester: rankWith(2, or(isObjectArrayControl, isPrimitiveArrayControl)), renderer: ArrayRendererComponent },
    { tester: rankWith(3, or(isEnumControl, isOneOfEnumControl, hasTitleMap)), renderer: SelectControlComponent },
    // Controls chosen by the ajsf widget type.
    { tester: rankWith(10, widgetIs('text', 'number', 'integer', 'email', 'url', 'tel', 'password', 'color', 'time', 'datetime-local', 'month', 'week', 'search')), renderer: InputControlComponent },
    { tester: rankWith(10, widgetIs('checkbox')), renderer: CheckboxControlComponent },
    { tester: rankWith(10, widgetIs('textarea')), renderer: TextareaControlComponent },
    { tester: rankWith(10, widgetIs('select')), renderer: SelectControlComponent },
    { tester: rankWith(10, widgetIs('date')), renderer: DateControlComponent },
    { tester: rankWith(10, widgetIs('currency')), renderer: CurrencyControlComponent },
    { tester: rankWith(10, widgetIs('file')), renderer: FileControlComponent },
    { tester: rankWith(10, widgetIs('lookup')), renderer: LookupControlComponent },
    { tester: rankWith(10, widgetIs('lookup-selector')), renderer: LookupSelectorControlComponent },
    { tester: rankWith(10, widgetIs('wysiwyg')), renderer: WysiwygControlComponent },
    { tester: rankWith(10, widgetIs('href')), renderer: HrefControlComponent },
];
