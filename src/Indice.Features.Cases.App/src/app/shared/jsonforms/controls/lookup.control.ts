import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LookupItemResultSet } from 'src/app/core/services/cases-api.service';
import { LookupsService } from 'src/app/core/services/lookups.service';
import { CaseFormControl } from './case-form-control';
import { FieldWrapperComponent } from './field-wrapper.component';

/** ajsf `lookup` widget: a text input with the lookup items ("value - name") as suggestions; the field holds the value. */
@Component({
    selector: 'app-jf-lookup',
    imports: [FieldWrapperComponent, FormsModule],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        <input
          type="text"
          autocomplete="off"
          [class]="fieldClass"
          [attr.list]="id + 'Autocomplete'"
          [attr.placeholder]="options.placeholder"
          [attr.required]="options.required"
          [attr.aria-invalid]="invalid"
          [attr.aria-describedby]="describedBy"
          [id]="id"
          [readonly]="readonly"
          [(ngModel)]="searchTerm"
          (change)="commit()">
        <datalist [id]="id + 'Autocomplete'">
          @for (word of source; track word) {
            <option [value]="word"></option>
          }
        </datalist>
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class LookupControlComponent extends CaseFormControl implements OnInit {
    protected widget = 'lookup';
    private lookupsService = inject(LookupsService);
    private changeDetector = inject(ChangeDetectorRef);
    private readonly separator = '-';
    private lastData: any;
    source: string[] = [];
    searchTerm: string | undefined;

    override ngOnInit(): void {
        super.ngOnInit();
        const lookupName = this.options['lookup-name'] ?? this.controlName;
        this.addSubscription(this.lookupsService.getLookup(lookupName).subscribe((lookUpItems: LookupItemResultSet) => {
            this.source = (lookUpItems?.items ?? []).map(item => `${item.value} ${this.separator} ${item.name}`);
            this.searchTerm = this.findTerm(this.data);
            this.changeDetector.detectChanges();
        }));
    }

    protected override onStateChange(): void {
        if (this.data !== this.lastData) {
            this.lastData = this.data;
            this.searchTerm = this.findTerm(this.data);
        }
    }

    commit(): void {
        if (!this.searchTerm || !this.source.includes(this.searchTerm)) {
            this.searchTerm = '';
        }
        this.setValue(this.searchTerm.substring(0, this.searchTerm.indexOf(' ')));
    }

    private findTerm(value: any): string | undefined {
        return value !== undefined && value !== null ? this.source.find(s => s.startsWith(`${value} ${this.separator}`)) : value;
    }
}
