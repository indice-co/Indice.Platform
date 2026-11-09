import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CaseTypeFormState } from './case-type-form.state';
import { CaseTypeJsonFieldComponent } from './case-type-json-field.component';

/** The case type configuration and the configuration of its cases list (filters, columns). */
@Component({
  selector: 'app-case-type-configuration',
  imports: [TranslatePipe, CaseTypeJsonFieldComponent],
  host: { class: 'block' },
  template: `
    <section class="panel p-5 sm:p-6">
      <fieldset class="fieldset grid gap-x-6 gap-y-3 lg:grid-cols-2">
        <legend class="fieldset-legend text-base">{{ 'caseTypes.form.sectionConfig' | translate }}</legend>
        <app-case-type-json-field class="lg:col-span-2" [state]="state()" field="config" label="caseTypes.form.config" [rows]="10" />
        <app-case-type-json-field [state]="state()" field="gridFilterConfig" label="caseTypes.form.gridFilterConfig" [rows]="8" />
        <app-case-type-json-field [state]="state()" field="gridColumnConfig" label="caseTypes.form.gridColumnConfig" [rows]="8" />
      </fieldset>
    </section>
  `
})
export class CaseTypeConfigurationComponent {
  public readonly state = input.required<CaseTypeFormState>();
}
