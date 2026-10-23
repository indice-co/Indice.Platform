import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { composePaths, getAjv, getConfig, getData, getSchema, isVisible, JsonFormsState, Resolve, toDataPath, UISchemaElement } from '@jsonforms/core';
import { get } from 'lodash';
import { TranslateService } from '@ngx-translate/core';
import { LucideExternalLink } from '@lucide/angular';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { addClasses, presentField } from '../jsonforms-utilities';

/**
 * ajsf `label-only` widget: shows a value as text (enums by name, booleans as yes / no, `extraType: currency`),
 * or a link built from the form data (`extraType: "data-bind href"`, `data: "{baseAddress}/api/{id}"`).
 * Used for `Control`s with `options.widget: label-only` and for `LabelOnly` elements (no `key` in the layout).
 */
@Component({
    selector: 'app-jf-label-only',
    imports: [LucideExternalLink],
    template: `
    @if (shown) {
      <div [class]="wrapperClass">
        @if (title) {
          <p class="text-sm font-medium text-base-content/70" [class]="options.labelHtmlClass || ''" [class.sr-only]="notitle" [innerHTML]="title"></p>
        }
        @if (!options.extraType?.includes('href')) {
          <p class="text-sm break-words" [class]="displayValue === '-' ? 'text-base-content/50' : 'font-medium'">{{ displayValue }}</p>
        } @else {
          <a [href]="displayValue" [title]="title" class="link link-primary inline-flex items-center gap-1 text-sm" [class]="options.fieldHtmlClass || ''"
            target="_blank" rel="noopener">{{ options.linkText }}<svg lucideExternalLink class="size-3.5"></svg></a>
        }
      </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class LabelOnlyRendererComponent extends JsonFormsBaseRenderer<UISchemaElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    private translate = inject(TranslateService);
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
        this.wrapperClass = addClasses(this.options.htmlClass, 'schema-form-label-only', 'flex flex-col gap-0.5 py-1.5');
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
            value = this.translate.instant(value ? 'common.yes' : 'common.no');
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
