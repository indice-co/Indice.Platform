import {
    and, isBooleanControl, isControl, isDateControl, isDateTimeControl, isEnumControl, isIntegerControl, isMultiLineControl, isNumberControl,
    isObjectArray, isObjectArrayControl, isObjectControl, isOneOfEnumControl, isPrimitiveArrayControl, isRangeControl, isStringControl,
    isTimeControl, JsonFormsRendererRegistryEntry, or, rankWith, Tester, uiTypeIs
} from '@jsonforms/core';
import { CheckboxControlComponent } from './controls/checkbox.control';
import { CurrencyControlComponent } from './controls/currency.control';
import { DateTimeControlComponent } from './controls/date-time.control';
import { DateControlComponent } from './controls/date.control';
import { EnumArrayControlComponent, isEnumArrayControl } from './controls/enum-array.control';
import { FileControlComponent } from './controls/file.control';
import { HrefControlComponent } from './controls/href.control';
import { ImageUriControlComponent } from './controls/image-uri.control';
import { InputControlComponent } from './controls/input.control';
import { LookupSelectorControlComponent } from './controls/lookup-selector.control';
import { LookupControlComponent } from './controls/lookup.control';
import { RangeControlComponent } from './controls/range.control';
import { SelectControlComponent } from './controls/select.control';
import { TextareaControlComponent } from './controls/textarea.control';
import { WysiwygControlComponent } from './controls/wysiwyg.control';
import { DisplayLayoutRendererComponent } from './display/display-layout.renderer';
import { DisplayFileControlComponent, DisplayImageUriControlComponent, DisplayLookupControlComponent, DisplayLookupSelectorControlComponent } from './display/display-widgets';
import { DisplayControlComponent } from './display/display.control';
import { HtmlRendererComponent } from './elements/html.renderer';
import { LabelOnlyRendererComponent } from './elements/label-only.renderer';
import { LabelRendererComponent } from './elements/label.renderer';
import { SubmitRendererComponent } from './elements/submit.renderer';
import { UnknownRendererComponent } from './elements/unknown.renderer';
import { ArrayRendererComponent } from './layouts/array.renderer';
import { CategorizationRendererComponent } from './layouts/categorization.renderer';
import { CombinatorRendererComponent, isCombinatorControl } from './layouts/combinator.renderer';
import { LayoutRendererComponent } from './layouts/layout.renderer';
import { ListWithDetailRendererComponent } from './layouts/list-with-detail.renderer';
import { ObjectRendererComponent } from './layouts/object.renderer';
import { isTableArrayControl, TableRendererComponent } from './layouts/table.renderer';

/** A control whose layout item had one of these ajsf widget types (`options.widget`). */
const widgetIs = (...widgets: string[]): Tester => and(isControl, uischema => widgets.includes(uischema.options?.['widget']));
const hasTitleMap: Tester = and(isControl, uischema => !!uischema.options?.['titleMap']);
const isListWithDetail: Tester = and(uiTypeIs('ListWithDetail'), isObjectArray);
const isLayout: Tester = or(uiTypeIs('VerticalLayout'), uiTypeIs('HorizontalLayout'), uiTypeIs('Group'), uiTypeIs('Category'));

/** Renderers shared by the form and the read-only view: structure, static content, and a fallback for anything else. */
const commonRenderers: JsonFormsRendererRegistryEntry[] = [
    { tester: rankWith(1, uiTypeIs('Categorization')), renderer: CategorizationRendererComponent },
    { tester: rankWith(1, uiTypeIs('Html')), renderer: HtmlRendererComponent },
    { tester: rankWith(1, uiTypeIs('Label')), renderer: LabelRendererComponent },
    { tester: rankWith(10, or(uiTypeIs('LabelOnly'), widgetIs('label-only'))), renderer: LabelOnlyRendererComponent },
    { tester: rankWith(2, isObjectControl), renderer: ObjectRendererComponent },
    { tester: rankWith(2, isObjectArrayControl), renderer: ArrayRendererComponent },
    { tester: rankWith(3, isCombinatorControl), renderer: CombinatorRendererComponent },
    { tester: rankWith(4, isTableArrayControl), renderer: TableRendererComponent },
    { tester: rankWith(4, isListWithDetail), renderer: ListWithDetailRendererComponent },
    { tester: rankWith(0, () => true), renderer: UnknownRendererComponent },
];

/**
 * The Tailwind renderer set for `<jsonforms [renderers]>`. A control with an ajsf widget type (`options.widget`) gets that widget (rank 10);
 * otherwise the schema decides, as in ajsf: enum / titleMap -> select, boolean -> checkbox, `format: date` -> date, string / number -> input.
 * The options of the JSON Forms angular-material set work as well: `multi` (textarea), `toggle`, `slider`, and the `ListWithDetail` element.
 */
export const tailwindRenderers: JsonFormsRendererRegistryEntry[] = [
    ...commonRenderers,
    // Layouts and elements.
    { tester: rankWith(1, isLayout), renderer: LayoutRendererComponent },
    { tester: rankWith(1, uiTypeIs('Submit')), renderer: SubmitRendererComponent },
    // Controls chosen by the schema.
    { tester: rankWith(1, or(isStringControl, isNumberControl, isIntegerControl)), renderer: InputControlComponent },
    { tester: rankWith(2, isBooleanControl), renderer: CheckboxControlComponent },
    { tester: rankWith(2, isDateControl), renderer: DateControlComponent },
    { tester: rankWith(2, or(isDateTimeControl, isTimeControl)), renderer: DateTimeControlComponent },
    { tester: rankWith(2, isPrimitiveArrayControl), renderer: ArrayRendererComponent },
    { tester: rankWith(3, and(isStringControl, isMultiLineControl)), renderer: TextareaControlComponent },
    { tester: rankWith(3, or(isEnumControl, isOneOfEnumControl, hasTitleMap)), renderer: SelectControlComponent },
    { tester: rankWith(4, isRangeControl), renderer: RangeControlComponent },
    { tester: rankWith(5, isEnumArrayControl), renderer: EnumArrayControlComponent },
    // Controls chosen by the ajsf widget type.
    { tester: rankWith(10, widgetIs('text', 'number', 'integer', 'email', 'url', 'tel', 'password', 'color', 'time', 'datetime-local', 'month', 'week', 'search')), renderer: InputControlComponent },
    { tester: rankWith(10, widgetIs('checkbox', 'toggle')), renderer: CheckboxControlComponent },
    { tester: rankWith(10, widgetIs('textarea')), renderer: TextareaControlComponent },
    { tester: rankWith(10, widgetIs('select')), renderer: SelectControlComponent },
    { tester: rankWith(10, widgetIs('date')), renderer: DateControlComponent },
    { tester: rankWith(10, widgetIs('currency')), renderer: CurrencyControlComponent },
    { tester: rankWith(10, widgetIs('file')), renderer: FileControlComponent },
    { tester: rankWith(10, widgetIs('lookup')), renderer: LookupControlComponent },
    { tester: rankWith(10, widgetIs('lookup-selector')), renderer: LookupSelectorControlComponent },
    { tester: rankWith(10, widgetIs('wysiwyg')), renderer: WysiwygControlComponent },
    { tester: rankWith(10, widgetIs('href')), renderer: HrefControlComponent },
    { tester: rankWith(10, and(isStringControl, widgetIs('imageUri'))), renderer: ImageUriControlComponent },
];

/**
 * The renderer set of the read-only case view: the same structure, with label / value pairs instead of inputs.
 * Every primitive control (and every array of values) is the one display control; attachments and lookups keep their own logic.
 */
export const displayRenderers: JsonFormsRendererRegistryEntry[] = [
    ...commonRenderers,
    { tester: rankWith(1, isLayout), renderer: DisplayLayoutRendererComponent },
    { tester: rankWith(1, isControl), renderer: DisplayControlComponent },
    { tester: rankWith(3, or(isPrimitiveArrayControl, isEnumArrayControl)), renderer: DisplayControlComponent },
    {
        tester: rankWith(10, widgetIs('text', 'number', 'integer', 'email', 'url', 'tel', 'password', 'color', 'time', 'datetime-local', 'month', 'week', 'search',
            'checkbox', 'toggle', 'textarea', 'select', 'date', 'currency', 'wysiwyg', 'href')),
        renderer: DisplayControlComponent
    },
    { tester: rankWith(10, widgetIs('file')), renderer: DisplayFileControlComponent },
    { tester: rankWith(10, widgetIs('lookup')), renderer: DisplayLookupControlComponent },
    { tester: rankWith(10, widgetIs('lookup-selector')), renderer: DisplayLookupSelectorControlComponent },
    { tester: rankWith(10, and(isStringControl, widgetIs('imageUri'))), renderer: DisplayImageUriControlComponent },
];
