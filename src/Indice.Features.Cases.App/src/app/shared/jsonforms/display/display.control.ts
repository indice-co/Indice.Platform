import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LucideExternalLink } from '@lucide/angular';
import moment from 'moment';
import { formatDateTimeValue } from '../controls/date-time.control';
import { enumArrayOptions } from '../controls/enum-array.control';
import { textOf } from '../controls/select.control';
import { TailwindControl } from '../controls/tailwind-control';
import { addClasses, buildTitleMap, formatCurrency, isEmptyValue } from '../jsonforms-utilities';
import { DisplayFieldComponent } from './display-field.component';

type DisplayKind = 'text' | 'list' | 'link' | 'html' | 'color';

/**
 * Every primitive field of the read-only case view (and arrays of primitives): the value as text, formatted the way its edit control
 * shows it: enum / titleMap names, yes / no, dd/MM/yyyy dates, Greek currency amounts, links, rich text, colour swatches, value lists.
 */
@Component({
    selector: 'app-jf-display',
    imports: [DisplayFieldComponent, LucideExternalLink],
    template: `
    @if (shown) {
      <app-jf-display-field [class]="wrapperClass" [label]="notitle ? null : title" [text]="text" [custom]="kind !== 'text' && !empty">
        @switch (kind) {
          @case ('list') {
            <ul class="flex flex-wrap gap-1.5">
              @for (item of items; track $index) {
                <li class="badge badge-soft badge-sm h-auto py-0.5">{{ item }}</li>
              }
            </ul>
          }
          @case ('link') {
            <a [href]="data" class="link link-primary inline-flex items-center gap-1 break-all" target="_blank" rel="noopener">
              {{ data }}<svg lucideExternalLink class="size-3.5 shrink-0"></svg>
            </a>
          }
          @case ('html') {
            <div class="prose prose-sm max-w-none text-base-content" [innerHTML]="data"></div>
          }
          @case ('color') {
            <span class="inline-flex items-center gap-2 font-medium">
              <span class="size-4 rounded-sm border border-base-300" [style.background]="data"></span>{{ data }}
            </span>
          }
        }
      </app-jf-display-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class DisplayControlComponent extends TailwindControl {
    private translate = inject(TranslateService);
    protected widget = 'display';
    // a view has no required marker on its labels
    override showLabel = false;
    kind: DisplayKind = 'text';
    empty = true;
    text = '';
    items: string[] = [];

    override get wrapperClass(): string {
        return addClasses(this.options.htmlClass, 'schema-form-display');
    }

    override get readonlyText(): string {
        return this.kind === 'list' ? this.items.join(', ') : this.text;
    }

    protected override onStateChange(): void {
        const value = this.data;
        const widget = this.options.widget;
        const format = this.options.format;
        this.empty = isEmptyValue(value);
        this.text = '';
        this.items = [];
        if (Array.isArray(value)) {
            this.kind = 'list';
            const names = enumArrayOptions(this.scopedSchema, this.rootSchema, this.options);
            this.items = value.map(item => {
                const name = names.find(n => n.value === item)?.name;
                return name !== undefined ? textOf(name) : this.formatScalar(item, this.scopedSchema?.items ?? {});
            });
        } else if (widget === 'wysiwyg') {
            this.kind = 'html';
        } else if ((widget === 'href' || widget === 'url' || format === 'uri') && /^https?:\/\//i.test(`${value}`)) {
            this.kind = 'link';
        } else if (widget === 'color' || format === 'color') {
            this.kind = 'color';
        } else {
            this.kind = 'text';
            this.text = this.empty ? '' : this.formatScalar(value, this.options);
        }
    }

    private formatScalar(value: any, options: any): string {
        if (value === undefined || value === null || value === '') {
            return '';
        }
        if (typeof value === 'boolean') {
            return this.translate.instant(value ? 'common.yes' : 'common.no');
        }
        const names = buildTitleMap(options);
        const name = names.find(n => n.value === value)?.name;
        if (name !== undefined) {
            return textOf(name);
        }
        if (options.widget === 'currency' || options.extraType === 'currency') {
            return formatCurrency(value);
        }
        if (options.widget === 'date' || options.format === 'date') {
            const date = moment(value, 'YYYY-MM-DD', true);
            return date.isValid() ? date.format('DD/MM/YYYY') : `${value}`;
        }
        if (options.format === 'date-time' || options.format === 'time') {
            return formatDateTimeValue(value, options.format);
        }
        if (typeof value === 'object') {
            return JSON.stringify(value);
        }
        return `${value}`;
    }
}
