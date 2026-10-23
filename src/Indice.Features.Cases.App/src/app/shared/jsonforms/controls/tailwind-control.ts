import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsControl } from '@jsonforms/angular';
import { Actions, getData, getErrorAt, JsonFormsState, StatePropsOfControl } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, ErrorObject, formatErrors, presentField } from '../jsonforms-utilities';

/**
 * Base of the control renderers. Exposes the ajsf-like `options`, label, css classes,
 * visibility (`condition`, `case-channel`), error and help text.
 *
 * Change detection: JSON Forms' base classes update plain fields from a `$state` subscription and never mark
 * the view for check, so every renderer keeps `ChangeDetectionStrategy.Eager`.
 */
// a component, not a directive: JSON Forms' JsonFormsAbstractControl is a component and directives cannot extend components
@Component({ template: '', changeDetection: ChangeDetectionStrategy.Eager })
export abstract class TailwindControl extends JsonFormsControl {
    protected caseDetailsService = inject(CaseDetailsService);
    /** The ajsf widget name, used for the `schema-form-<widget>` css class. */
    protected abstract widget: string;
    /** The daisyUI class of the editor element (`input`, `select`, `textarea`...). */
    protected fieldBase = 'input';
    /** Whether the wrapper shows the label above the control (checkboxes draw their own). */
    showLabel = true;
    /** In read-only mode the wrapper shows {@link readonlyText} instead of the editor. */
    readonlyAsText = true;
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    /** Validation message (html), shown once the user changed the value (as ajsf did for dirty controls). */
    errorText: string | null = null;
    /** Description / help text (html). */
    helpText: string | null = null;
    rootData: any;
    /** The user changed the value; errors are shown from then on. */
    dirty = false;
    private errorObjects: ErrorObject[] = [];

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, `schema-form-${this.widget}`, 'fieldset py-1.5');
    }

    get labelClass(): string {
        return addClasses(this.options.labelHtmlClass, 'fieldset-legend pt-0 pb-1 text-sm font-medium');
    }

    get fieldClass(): string {
        return addClasses(this.options.fieldHtmlClass, this.fieldBase, 'w-full', this.errorText && `${this.fieldBase}-error`);
    }

    /** Hides the editor in favour of the read-only text block. */
    get showEditor(): boolean {
        return !(this.readonly && this.readonlyAsText);
    }

    /** The value as text for read-only mode. */
    get readonlyText(): string {
        return this.data === undefined || this.data === null || this.data === '' ? '' : `${this.data}`;
    }

    /** The last segment of the data path (ajsf `controlName`). */
    get controlName(): string {
        const segments = (this.propsPath ?? '').split('.');
        return segments[segments.length - 1];
    }

    protected override mapToProps(state: JsonFormsState): StatePropsOfControl {
        this.rootData = getData(state);
        const props = super.mapToProps(state);
        this.errorObjects = getErrorAt(props.path, props.schema)(state);
        return props;
    }

    override mapAdditionalProps(props: StatePropsOfControl): void {
        const field = presentField(this.uischema, props.schema, props.path, this.rootData, props, this.caseDetailsService.caseDetails?.channel, this.showLabel);
        this.options = field.options;
        this.title = field.title;
        this.notitle = field.notitle;
        this.readonly = field.readonly;
        this.shown = field.shown;
        this.errorText = this.dirty && this.errorObjects.length ? formatErrors(this.errorObjects, this.options.validationMessages) : null;
        this.helpText = this.options.description || this.options.help || null;
        this.onStateChange();
    }

    /** Called after every state change, once the field properties are updated. */
    protected onStateChange(): void { }

    /** Writes a value to the form data. Empty strings are stored as `undefined`, as ajsf removed them. */
    protected setValue(value: any): void {
        this.dirty = true;
        this.jsonFormsService.updateCore(Actions.update(this.propsPath, () => value === '' || value === null ? undefined : value));
        this.triggerValidation();
    }
}
