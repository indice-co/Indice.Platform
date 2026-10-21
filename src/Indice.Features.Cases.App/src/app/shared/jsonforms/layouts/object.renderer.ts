import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsControlWithDetail, JsonFormsOutlet } from '@jsonforms/angular';
import { findUISchema, Generate, getData, JsonFormsState, JsonSchema, OwnPropsOfRenderer, setReadonly, StatePropsOfControlWithDetail } from '@jsonforms/core';
import { cloneDeep } from 'lodash';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { presentField } from '../jsonforms-utilities';

/**
 * A `Control` bound to an object property (what JSON Forms generates for nested objects): the object's own UI schema
 * (`options.detail`, a registered one, or a generated `Group` titled after the property) rendered at the object's data path.
 */
@Component({
    selector: 'app-jf-object',
    imports: [JsonFormsOutlet],
    template: `
    @if (shown && detail) {
      <jsonforms-outlet [renderProps]="detail"></jsonforms-outlet>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class ObjectRendererComponent extends JsonFormsControlWithDetail {
    private caseDetailsService = inject(CaseDetailsService);
    private rootData: any;
    private detailSchema: JsonSchema | undefined;
    shown = true;
    detail: OwnPropsOfRenderer | undefined;

    protected override mapToProps(state: JsonFormsState) {
        this.rootData = getData(state);
        return super.mapToProps(state);
    }

    override mapAdditionalProps(props: StatePropsOfControlWithDetail): void {
        const field = presentField(this.uischema, props.schema, props.path, this.rootData, props, this.caseDetailsService.caseDetails?.channel);
        this.shown = field.shown;
        // built once per schema: a new UI schema object would re-create every field below (and lose the focus)
        if (this.detail && this.detailSchema === props.schema) {
            return;
        }
        this.detailSchema = props.schema;
        const uischema: any = cloneDeep(findUISchema(
            props.uischemas ?? [],
            props.schema,
            this.uischema.scope,
            props.path,
            () => {
                // combinators of the object itself have no place in a group of its properties
                const { oneOf, anyOf, allOf, ...propertiesOnly } = props.schema;
                return Generate.uiSchema(propertiesOnly, 'Group', undefined, props.rootSchema);
            },
            this.uischema,
            props.rootSchema));
        if (uischema.type === 'Group' && !uischema.label && !field.notitle && field.title) {
            uischema.label = field.title;
        }
        if (field.readonly) {
            setReadonly(uischema);
        }
        this.detail = { uischema, schema: props.schema, path: props.path };
    }
}
