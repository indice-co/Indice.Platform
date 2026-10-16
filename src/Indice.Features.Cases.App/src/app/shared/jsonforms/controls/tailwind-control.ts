import { Component, inject } from '@angular/core';
import { JsonFormsControl } from '@jsonforms/angular';
import { Actions, getData, getErrorAt, JsonFormsState, StatePropsOfControl } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, ErrorObject, formatErrors, presentField } from '../jsonforms-utilities';

/**
 * Base of the Tailwind control renderers. Exposes the ajsf-like `options`, label, css classes,
 * visibility (`condition`, `case-channel`) and help block that the ajsf Tailwind framework provided.
 */
// a component, not a directive: JSON Forms' JsonFormsAbstractControl is a component and directives cannot extend components
@Component({ template: '' })
export abstract class TailwindControl extends JsonFormsControl {
    protected caseDetailsService = inject(CaseDetailsService);
    /** The ajsf widget name, used for the `schema-form-<widget>` css class. */
    protected abstract widget: string;
    /** Whether the wrapper shows the label above the control (checkboxes draw their own). */
    showLabel = true;
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    helpBlock: string | null = null;
    rootData: any;
    /** The user changed the value; errors are shown from then on (ajsf showed them for dirty controls). */
    dirty = false;
    private errorObjects: ErrorObject[] = [];

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, `schema-form-${this.widget}`, 'form-group');
    }

    get labelClass(): string {
        return addClasses(this.options.labelHtmlClass, 'form-label inline-block mb-1 text-gray-700');
    }

    get fieldClass(): string {
        return addClasses(this.options.fieldHtmlClass, 'form-control', this.readonly && 'read-only:bg-gray-100');
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
        this.helpBlock = this.dirty && this.errorObjects.length
            ? formatErrors(this.errorObjects, this.options.validationMessages)
            : this.options.description || this.options.help || null;
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
