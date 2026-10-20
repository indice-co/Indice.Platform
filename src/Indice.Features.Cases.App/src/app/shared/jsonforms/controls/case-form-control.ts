import { Component, inject } from '@angular/core';
import { JsonFormsControl } from '@jsonforms/angular';
import { Actions, getData, getErrorAt, JsonFormsState, StatePropsOfControl } from '@jsonforms/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, ErrorObject, formatErrors, presentField } from '../jsonforms-utilities';

/** The DaisyUI element class a control draws its field with. */
export type FieldKind = 'input' | 'select' | 'textarea' | 'file-input' | 'checkbox';

// Spelled out so Tailwind finds the class names in source and DaisyUI generates them.
const ERROR_CLASS: Record<FieldKind, string> = {
    'input': 'input-error',
    'select': 'select-error',
    'textarea': 'textarea-error',
    'file-input': 'file-input-error',
    'checkbox': 'checkbox-error'
};

/**
 * Base of the case-form controls. Exposes the ajsf-like `options`, label, css classes, visibility (`condition`,
 * `case-channel`), read-only state and the help / error line that case-type layouts were written against.
 * Layout classes (`htmlClass`, `labelHtmlClass`, `fieldHtmlClass`) are appended after the built-in ones.
 */
// A component, not a directive: JSON Forms' JsonFormsAbstractControl is a component and directives cannot extend components.
@Component({
    template: '',
    host: { '[class.jf-hidden]': '!shown' }
})
export abstract class CaseFormControl extends JsonFormsControl {
    protected caseDetailsService = inject(CaseDetailsService);
    /** The ajsf widget name, used for the `schema-form-<widget>` hook class. */
    protected abstract widget: string;
    /** The DaisyUI class of the field element. */
    protected fieldKind: FieldKind = 'input';
    /** Whether the wrapper shows the label above the control (checkboxes draw their own). */
    showLabel = true;
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;
    /** Validation messages (once the field is dirty) or the field description. HTML. */
    helpBlock: string | null = null;
    /** `helpBlock` holds validation errors. */
    invalid = false;
    rootData: any;
    /** The user changed the value; errors are shown from then on (as ajsf did for dirty controls). */
    dirty = false;
    private errorObjects: ErrorObject[] = [];

    get wrapperClass(): string {
        return addClasses('jf-field', 'form-group', `schema-form-${this.widget}`, this.options.htmlClass);
    }

    get labelClass(): string {
        return addClasses('jf-label', this.options.labelHtmlClass);
    }

    get fieldClass(): string {
        return addClasses(
            this.fieldKind,
            this.fieldKind !== 'checkbox' && 'w-full',
            'form-control',
            this.readonly && 'jf-readonly',
            this.invalid && ERROR_CLASS[this.fieldKind],
            this.options.fieldHtmlClass
        );
    }

    get helpId(): string {
        return `${this.id}-help`;
    }

    /** `aria-describedby` of the field element. */
    get describedBy(): string | null {
        return this.helpBlock ? this.helpId : null;
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
        this.invalid = this.dirty && this.errorObjects.length > 0;
        this.helpBlock = this.invalid
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
