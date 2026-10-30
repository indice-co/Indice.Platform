import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CaseTypeFormState, CaseTypeGeneralModel } from './case-type-form.state';

/** First part of the case type form: code, title, ordering, tags, who can create it and whether it gets a menu entry. */
@Component({
  selector: 'app-case-type-general',
  imports: [TranslatePipe],
  host: { class: 'block' },
  template: `
    @let general = state().general();
    <section class="panel p-5 sm:p-6">
      <fieldset class="fieldset grid gap-x-6 gap-y-3 md:grid-cols-2">
        <legend class="fieldset-legend text-base">{{ 'caseTypes.form.legend' | translate }}</legend>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.code' | translate }} <span class="text-error">*</span></span>
          <input class="input w-full font-mono" type="text" maxlength="128" required
            [placeholder]="'caseTypes.form.codePlaceholder' | translate" [value]="general.code" (input)="set('code', value($event))" />
        </label>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.titleLabel' | translate }} <span class="text-error">*</span></span>
          <input class="input w-full" type="text" maxlength="128" required
            [placeholder]="'caseTypes.form.titlePlaceholder' | translate" [value]="general.title" (input)="set('title', value($event))" />
        </label>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.order' | translate }}</span>
          <input class="input w-full" type="number" [value]="general.order" (input)="set('order', value($event))" />
        </label>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.description' | translate }}</span>
          <input class="input w-full" type="text"
            [placeholder]="'caseTypes.form.descriptionPlaceholder' | translate" [value]="general.description" (input)="set('description', value($event))" />
        </label>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.tags' | translate }}</span>
          <input class="input w-full" type="text" placeholder="tag1,tag2" [value]="general.tags" (input)="set('tags', value($event))" />
        </label>
        <label class="w-full">
          <span class="label mb-1">{{ 'caseTypes.form.canCreateRoles' | translate }}</span>
          <input class="input w-full font-mono" type="text" placeholder="RoleA,RoleB" [value]="general.canCreateRoles" (input)="set('canCreateRoles', value($event))" />
        </label>
        <label class="label mt-2 cursor-pointer gap-3 text-base-content md:col-span-2">
          <input type="checkbox" class="toggle toggle-primary toggle-sm" [checked]="general.isMenuItem" (change)="set('isMenuItem', checked($event))" />
          <span>
            <span class="block font-medium">{{ 'caseTypes.form.isMenuItem' | translate }}</span>
            <span class="block text-xs text-base-content/60">{{ 'caseTypes.form.isMenuItemHint' | translate }}</span>
          </span>
        </label>
      </fieldset>
    </section>
  `
})
export class CaseTypeGeneralComponent {
  public readonly state = input.required<CaseTypeFormState>();

  protected set<K extends keyof CaseTypeGeneralModel>(key: K, value: CaseTypeGeneralModel[K]): void {
    this.state().setGeneral(key, value);
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected checked(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }
}
