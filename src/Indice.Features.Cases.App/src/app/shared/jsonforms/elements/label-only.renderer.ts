import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { composePaths, getAjv, getConfig, getData, getSchema, isVisible, JsonFormsState, Resolve, toDataPath, UISchemaElement } from '@jsonforms/core';
import { get } from 'lodash';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/**
 * ajsf `label-only` widget: shows a value as text (enums by name, booleans as Ναι / Όχι, `extraType: currency`),
 * or a link built from the form data (`extraType: "data-bind href"`, `data: "{baseAddress}/api/{id}"`).
 * Used for `Control`s with `options.widget: label-only` and for `LabelOnly` elements (no `key` in the layout).
 */
@Component({
    selector: 'app-jf-label-only',
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <h2 class="block text-sm" [class]="options.labelHtmlClass || ''" [style.display]="notitle ? 'none' : ''" [innerHTML]="title"></h2>
        }
        @if (!options.extraType?.includes('href')) {
          <div class="mt-1">
            <p>{{ displayValue }}</p>
          </div>
        } @else {
          <a [href]="displayValue" [title]="title" [class]="options.fieldHtmlClass || ''" target="_blank">{{ options.linkText }}</a>
        }
      </div>
    }`,
    styles: ['h2 { margin-top: 20px; }'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LabelOnlyRendererComponent extends JsonFormsBaseRenderer<UISchemaElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    options: any = {};
    title: string | null = null;
    notitle = false;
    shown = true;
    displayValue: any;
    wrapperClass = '';

    ngOnInit(): void {
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => this.update(state)));
    }

    private update(state: JsonFormsState): void {
        const rootData = getData(state);
        const scope: string | undefined = (this.uischema as any).scope;
        const dataPath = scope ? composePaths(this.path, toDataPath(scope)) : this.path;
        const schema = scope ? Resolve.schema(this.schema, scope, getSchema(state)) : undefined;
        const visible = isVisible(this.uischema, rootData, this.path, getAjv(state), getConfig(state));
        const field = presentField(this.uischema, schema, dataPath, rootData, { visible, enabled: true }, this.caseDetailsService.caseDetails?.channel, false);
        this.options = field.options;
        this.title = field.title;
        this.notitle = field.notitle;
        this.shown = field.shown;
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-label-only', 'form-group');
        this.displayValue = this.resolveDisplayValue(scope ? get(rootData, dataPath) : undefined, rootData, schema?.type);
    }

    /** Same order of checks as the ajsf widget. */
    private resolveDisplayValue(value: any, rootData: any, dataType: any): any {
        const hasValue = (v: any) => v !== null && v !== undefined && v !== '';
        const shouldDisplay = this.options.extraType !== 'blankspace' && !this.notitle && !this.options.data;
        const hasEnum = Array.isArray(this.options.enum) && Array.isArray(this.options.enumNames) && this.options.enum.length === this.options.enumNames.length;
        let displayValue: any;
        if (shouldDisplay && !hasValue(value)) {
            if (dataType !== 'number') {
                value = '-';
            }
        } else if (this.options.extraType === 'currency') {
            value = parseFloat(value).toLocaleString('el');
        } else if (typeof value === 'boolean') {
            value = value ? 'Ναι' : 'Όχι';
        } else if (hasEnum) {
            (this.options.enum as any[]).forEach((enumValue, index) => {
                if (value === enumValue) {
                    displayValue = this.options.enumNames[index];
                }
            });
        } else if (this.options.extraType?.includes('data-bind')) {
            // Placeholders like '{id}' are replaced with the form data value of that name.
            value = (this.options.data as string)?.replace(/{([^}]+)}/g, (match, placeholder) =>
                rootData && Object.prototype.hasOwnProperty.call(rootData, placeholder) ? rootData[placeholder] : match);
        }
        if (!hasEnum) {
            displayValue = shouldDisplay && !hasValue(value) ? '-' : value;
        }
        return displayValue;
    }
}
