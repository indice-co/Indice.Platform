import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { JsonEditorComponent } from 'src/app/shared/components/json-editor/json-editor.component';
import { CaseTypeFormState, CaseTypeJsonField } from './case-type-form.state';

/** A JSON document of the case type, edited as text in a code editor. */
@Component({
  selector: 'app-case-type-json-field',
  imports: [TranslatePipe, JsonEditorComponent],
  host: { class: 'block w-full' },
  template: `
    <span class="label mb-1" [id]="labelId()">{{ label() | translate }} @if (required()) { <span class="text-error">*</span> }</span>
    @if (hint()) {
      <p class="mb-2 text-xs text-base-content/60">{{ hint()! | translate }}</p>
    }
    <app-json-editor [rows]="rows()" [invalid]="invalid()" [labelledBy]="labelId()"
      [value]="state().json()[field()]" (valueChange)="state().setJson(field(), $event)" />
    @if (invalid()) {
      <span class="mt-1 block text-xs text-error">{{ 'caseTypes.form.invalidJson' | translate }}</span>
    }
  `
})
export class CaseTypeJsonFieldComponent {
  public readonly state = input.required<CaseTypeFormState>();
  public readonly field = input.required<CaseTypeJsonField>();
  /** Translation key of the label. */
  public readonly label = input.required<string>();
  /** Translation key of a short explanation under the label. */
  public readonly hint = input<string>();
  public readonly required = input(false);
  public readonly rows = input(8);

  protected readonly labelId = computed(() => `case-type-${this.field()}`);
  protected readonly invalid = computed(() => this.state().invalidJson().includes(this.field()));
}
