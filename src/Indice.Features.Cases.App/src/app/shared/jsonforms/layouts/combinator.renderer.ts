import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonFormsControl, JsonFormsOutlet } from '@jsonforms/angular';
import {
    Actions, and, CombinatorKeyword, createCombinatorRenderInfos, createDefaultValue, getData, isAllOfControl, isAnyOfControl, isOneOfControl,
    isOneOfEnumControl, JsonFormsState, JsonSchema, mapStateToCombinatorRendererProps, not, or, OwnPropsOfRenderer, StatePropsOfControl, Tester
} from '@jsonforms/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, isEmptyValue, presentField } from '../jsonforms-utilities';

/** `oneOf` / `anyOf` / `allOf` controls; a `oneOf` of constants is an enum and goes to the select instead. */
export const isCombinatorControl: Tester = or(and(isOneOfControl, not(isOneOfEnumControl)), isAnyOfControl, isAllOfControl);

/**
 * Combinator controls. `oneOf` / `anyOf`: a tab per alternative, opened on the one the data matches; switching a `oneOf` with data
 * asks first, because the data of the other alternative is dropped. `allOf`: every part, one after the other.
 * Read-only: only the alternative the data matches.
 */
@Component({
    selector: 'app-jf-combinator',
    imports: [JsonFormsOutlet, TranslatePipe],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <p class="mb-2 text-sm font-medium" [class.sr-only]="notitle" [innerHTML]="title"></p>
        }
        @if (keyword === 'allOf') {
          @for (alternative of alternatives; track $index) {
            <jsonforms-outlet [renderProps]="alternative.props"></jsonforms-outlet>
          }
        } @else {
          @if (!readonly) {
            <div role="tablist" class="tabs tabs-box tabs-sm mb-3 w-fit">
              @for (alternative of alternatives; track $index) {
                <button type="button" role="tab" class="tab" [class.tab-active]="$index === selected" [attr.aria-selected]="$index === selected"
                  (click)="choose($index)">{{ alternative.label }}</button>
              }
            </div>
            @if (pending !== null) {
              <div role="alert" class="alert alert-warning alert-soft mb-3 flex-wrap text-sm">
                <span class="flex-1">{{ 'jsonforms.switchAlternative.message' | translate }}</span>
                <div class="flex gap-2">
                  <button type="button" class="btn btn-ghost btn-sm" (click)="pending = null">{{ 'common.cancel' | translate }}</button>
                  <button type="button" class="btn btn-warning btn-sm" (click)="switchTo(pending)">{{ 'jsonforms.switchAlternative.confirm' | translate }}</button>
                </div>
              </div>
            }
          }
          @if (alternatives[selected]; as alternative) {
            <jsonforms-outlet [renderProps]="alternative.props"></jsonforms-outlet>
          }
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class CombinatorRendererComponent extends JsonFormsControl {
    private caseDetailsService = inject(CaseDetailsService);
    private translate = inject(TranslateService);
    private rootData: any;
    private combinatorSchema: JsonSchema | undefined;
    private fittingIndex = -1;
    keyword: CombinatorKeyword = 'oneOf';
    alternatives: { label: string, schema: JsonSchema, props: OwnPropsOfRenderer }[] = [];
    selected = 0;
    /** The alternative waiting for the user to confirm the switch. */
    pending: number | null = null;
    options: any = {};
    title: string | null = null;
    notitle = false;
    readonly = false;
    shown = true;

    get wrapperClass(): string {
        return addClasses(this.options.htmlClass, `schema-form-${this.keyword}`, 'py-1.5');
    }

    protected override mapToProps(state: JsonFormsState): StatePropsOfControl {
        this.rootData = getData(state);
        const props = super.mapToProps(state);
        this.keyword = props.schema?.oneOf ? 'oneOf' : props.schema?.anyOf ? 'anyOf' : 'allOf';
        this.fittingIndex = mapStateToCombinatorRendererProps(state, this.getOwnProps(), this.keyword).indexOfFittingSchema;
        return props;
    }

    override mapAdditionalProps(props: StatePropsOfControl): void {
        const field = presentField(this.uischema, props.schema, props.path, this.rootData, props, this.caseDetailsService.caseDetails?.channel);
        this.options = field.options;
        this.title = field.title;
        this.notitle = field.notitle;
        this.readonly = field.readonly;
        this.shown = field.shown;
        // built once per schema: new render props would re-create every field below (and lose the focus)
        if (this.combinatorSchema === props.schema) {
            return;
        }
        this.combinatorSchema = props.schema;
        const infos = createCombinatorRenderInfos(props.schema[this.keyword] ?? [], props.rootSchema, this.keyword, this.uischema, props.path, (props as any).uischemas ?? []);
        this.alternatives = infos.map((info, i) => ({
            label: info.schema.title ?? this.translate.instant('jsonforms.alternative', { number: i + 1 }),
            schema: info.schema,
            props: { uischema: info.uischema, schema: info.schema, path: props.path }
        }));
        this.selected = Math.max(this.fittingIndex, 0);
    }

    choose(index: number): void {
        if (index === this.selected) {
            this.pending = null;
            return;
        }
        // anyOf alternatives may share data; a oneOf switch replaces it
        if (this.keyword === 'oneOf' && !isEmptyValue(this.data) && !(typeof this.data === 'object' && !Object.keys(this.data).length)) {
            this.pending = index;
            return;
        }
        this.switchTo(index);
    }

    switchTo(index: number): void {
        this.pending = null;
        this.selected = index;
        if (this.keyword === 'oneOf') {
            const value = createDefaultValue(this.alternatives[index].schema, this.rootSchema);
            this.jsonFormsService.updateCore(Actions.update(this.propsPath, () => value));
            this.triggerValidation();
        }
    }
}
