import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CaseTypeFormState } from './case-type-form.state';
import { CaseTypeJsonFieldComponent } from './case-type-json-field.component';
import { CaseTypeTranslationsComponent } from './case-type-translations.component';

/** What a case is made of: the data schema and its layout side by side, and the translations of the case type and its form texts below. */
@Component({
  selector: 'app-case-type-definition',
  imports: [TranslatePipe, CaseTypeJsonFieldComponent, CaseTypeTranslationsComponent],
  host: { class: 'block space-y-6' },
  template: `
    <section class="panel p-5 sm:p-6">
      <div class="grid gap-x-6 gap-y-5 *:min-w-0 lg:grid-cols-2">
        <app-case-type-json-field [state]="state()" field="dataSchema" label="caseTypes.form.schema"
          hint="caseTypes.form.schemaHint" [required]="true" [rows]="20" />
        <app-case-type-json-field [state]="state()" field="layout" label="caseTypes.form.layout"
          hint="caseTypes.form.layoutHint" [rows]="20" />
      </div>
    </section>
    <section class="panel p-5 sm:p-6">
      <h2 class="mb-5 flex items-center gap-2 text-base font-semibold">
        {{ 'caseTypes.form.sectionTranslations' | translate }}
        @if (!state().translationsValid()) {
          <span class="status status-error" [attr.aria-label]="'common.hasErrors' | translate"></span>
        }
      </h2>
      <app-case-type-translations [state]="state()" />
    </section>
  `
})
export class CaseTypeDefinitionComponent {
  public readonly state = input.required<CaseTypeFormState>();
}
