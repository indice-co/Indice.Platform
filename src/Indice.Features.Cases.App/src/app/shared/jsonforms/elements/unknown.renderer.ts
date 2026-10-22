import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { JsonFormsAngularService, JsonFormsBaseRenderer } from '@jsonforms/angular';
import { composePaths, getAjv, getConfig, getData, isVisible, JsonFormsState, toDataPath, UISchemaElement } from '@jsonforms/core';
import { TranslatePipe } from '@ngx-translate/core';
import { get } from 'lodash';
import { CaseDetailsService } from 'src/app/core/services/case-details.service';
import { arrayIndicesOf, evaluateCondition, fixTitle, isEmptyValue, isShownForChannel } from '../jsonforms-utilities';

/**
 * Last-resort renderer (rank 0) for an element no other renderer accepts, instead of JSON Forms' bare "No applicable renderer found".
 * Shows the value as JSON so that read-only cases never hide data.
 */
@Component({
    selector: 'app-jf-unknown',
    imports: [TranslatePipe],
    template: `
    @if (shown) {
    <div class="py-1.5">
      @if (title) {
        <p class="pb-1 text-sm font-medium">{{ title }}</p>
      }
      @if (json) {
        <pre class="overflow-x-auto rounded-field border border-base-300 bg-base-200/50 px-3 py-2 text-xs">{{ json }}</pre>
      }
      @if (!readonly) {
        <p class="mt-1 text-xs text-base-content/60">{{ 'jsonforms.unsupportedField' | translate }}</p>
      }
    </div>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class UnknownRendererComponent extends JsonFormsBaseRenderer<UISchemaElement> implements OnInit {
    private jsonFormsService = inject(JsonFormsAngularService);
    private caseDetailsService = inject(CaseDetailsService);
    title = '';
    json = '';
    readonly = false;
    shown = false;

    ngOnInit(): void {
        const scope: string | undefined = (this.uischema as any).scope;
        const label = (this.uischema as any).label;
        const options: any = this.uischema.options ?? {};
        this.title = typeof label === 'string' ? label : scope ? fixTitle(scope.split('/').pop() ?? '') : '';
        this.addSubscription(this.jsonFormsService.$state.subscribe((state: JsonFormsState) => {
            const data = getData(state);
            this.readonly = !!state.jsonforms.readonly;
            this.shown = isVisible(this.uischema, data, this.path, getAjv(state), getConfig(state))
                && isShownForChannel(options, this.caseDetailsService.caseDetails?.channel)
                && evaluateCondition(options.condition, data, arrayIndicesOf(this.path));
            // Never read the value of a hidden element, so hidden data cannot leak through the fallback
            const value = this.shown && scope ? get(data, composePaths(this.path, toDataPath(scope))) : undefined;
            this.json = isEmptyValue(value) ? '' : JSON.stringify(value, null, 2);
        }));
    }
}
