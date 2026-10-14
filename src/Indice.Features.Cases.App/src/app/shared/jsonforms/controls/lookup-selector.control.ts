import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FilterClause } from '@indice/ng-components';
import { cloneDeep } from 'lodash';
import { take } from 'rxjs/operators';
import { Case, CasesApiService, LookupItemResultSet } from 'src/app/core/services/cases-api.service';
import { LookupsService } from 'src/app/core/services/lookups.service';
import { isShownForChannel } from '../jsonforms-utilities';
import { TailwindControl } from './tailwind-control';

/**
 * ajsf `lookup-selector` widget: a select filled from a lookup, filtered by the case customer, `lookup-filter-terms`
 * and the current values of the `lookup-filter-fields` (reloaded when one of those fields changes).
 */
@Component({
    selector: 'app-jf-lookup-selector',
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (selectList.length > 0) {
          <select
            [class]="fieldClass"
            [attr.readonly]="readonly ? 'readonly' : null"
            [attr.required]="options.required"
            [id]="id"
            (change)="onSelect($event)">
            <option value="" [selected]="data === undefined || data === null"></option>
            @for (item of selectList; track $index) {
              @if (item.value) {
                <option [value]="$index" [selected]="item.value === data" [innerHTML]="item.name"></option>
              }
            }
          </select>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class LookupSelectorControlComponent extends TailwindControl {
    protected widget = 'lookup-selector';
    private lookupsService = inject(LookupsService);
    private api = inject(CasesApiService);
    private changeDetector = inject(ChangeDetectorRef);
    private initialized = false;
    /** The fields this field depends on, and their current values. */
    private lookupFilterFields: string[] | undefined;
    private lookupFilterFieldValues: { [field: string]: any } = {};
    /** The "constant" part of the filter terms. */
    private lookupFilterTerms: FilterClause[] = [];
    selectList: { name: string, value: any }[] = [];

    protected override onStateChange(): void {
        if (!this.initialized) {
            this.initialized = true;
            this.initialize();
            return;
        }
        if (!this.lookupFilterFields) {
            return;
        }
        // Did a field this field depends on change?
        let filterFieldValueChanged = false;
        for (const key in this.lookupFilterFieldValues) {
            if (this.rootData?.[key] && this.rootData[key] != this.lookupFilterFieldValues[key]) {
                filterFieldValueChanged = true;
                this.lookupFilterFieldValues[key] = this.rootData[key];
            }
        }
        if (filterFieldValueChanged) {
            // Deferred: updating the data while the state is being delivered would give stale state to the other renderers.
            queueMicrotask(() => this.setValue(undefined));
            this.addSubscription(this.api.getLookup(this.options['lookup-name'], undefined, undefined, undefined, undefined, this.addFilterFieldValuesToFilterTerms().map(x => x.toString()))
                .subscribe((lookUpItems: LookupItemResultSet) => this.createSelectList(lookUpItems)));
        }
    }

    private initialize(): void {
        // Hidden for the case channel: avoid the lookup call.
        if (this.options.condition === 'false' || !isShownForChannel(this.options, this.caseDetailsService.caseDetails?.channel)) {
            return;
        }
        this.lookupFilterFields = this.options['lookup-filter-fields'];
        this.lookupFilterFields?.forEach(field => this.lookupFilterFieldValues[field] = this.rootData?.[field]);
        // The case owner is the customer that filters the lookup.
        this.addSubscription(this.caseDetailsService.caseDetails$.pipe(take(1)).subscribe((caseDetails: Case) => {
            this.lookupFilterTerms = this.createFilterTerms(caseDetails.ownerId);
            const filterValues = Object.values(this.lookupFilterFieldValues);
            if (!this.lookupFilterFields || (filterValues.length > 0 && filterValues.every(value => value !== null && value !== undefined))) {
                this.addSubscription(this.lookupsService.getLookup(this.options['lookup-name'], this.addFilterFieldValuesToFilterTerms())
                    .pipe(take(1))
                    .subscribe((lookUpItems: LookupItemResultSet) => this.createSelectList(lookUpItems)));
            }
        }));
    }

    onSelect(event: Event): void {
        const index = (event.target as HTMLSelectElement).value;
        this.setValue(index === '' ? undefined : this.selectList[+index].value);
    }

    private addFilterFieldValuesToFilterTerms(): FilterClause[] {
        const lookupFilterTerms = [...this.lookupFilterTerms];
        if (this.lookupFilterFields) {
            for (const key in this.lookupFilterFieldValues) {
                lookupFilterTerms.push(new FilterClause(key, this.lookupFilterFieldValues[key], 'eq'));
            }
        }
        return lookupFilterTerms;
    }

    private createFilterTerms(customerId: string | undefined): FilterClause[] {
        let lookupFilterTerms: FilterClause[] = [];
        if (this.options['lookup-filter-terms']) {
            lookupFilterTerms = cloneDeep(this.options['lookup-filter-terms']).map((ft: any) => new FilterClause(ft.key, ft.value));
        }
        // Always send the customerId; it can be ignored server-side if not needed.
        lookupFilterTerms.push(new FilterClause('customerId', customerId, 'eq'));
        return lookupFilterTerms;
    }

    private createSelectList(lookUpItems: LookupItemResultSet): void {
        this.selectList = (lookUpItems?.items ?? []).map(item => ({ name: item.name!, value: item.value }));
        this.changeDetector.detectChanges();
    }
}
