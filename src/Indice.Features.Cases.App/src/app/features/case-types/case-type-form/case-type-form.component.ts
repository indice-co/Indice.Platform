import { Component, computed, effect, input, output, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ICaseTypeRequest } from 'src/app/core/services/cases-api.service';

type JsonField = 'dataSchema' | 'layout' | 'translations' | 'layoutTranslations' | 'config' | 'gridFilterConfig' | 'gridColumnConfig';

const JSON_FIELDS: JsonField[] = ['dataSchema', 'layout', 'translations', 'layoutTranslations', 'config', 'gridFilterConfig', 'gridColumnConfig'];

/**
 * The case type editor, shared by the create and edit pages. JSON properties are edited as text: each one is parsed as
 * the user types, an unparsable value is flagged inline and blocks saving, and the last valid value is what gets sent.
 */
@Component({
  selector: 'app-case-type-form',
  imports: [FormsModule, NgTemplateOutlet, NgIcon, TranslatePipe],
  template: `
    <form class="space-y-5" #form="ngForm" (ngSubmit)="submit()" novalidate autocomplete="on">
      <section class="panel" aria-labelledby="ct-basics">
        <div class="panel-head"><h2 id="ct-basics">{{ 'caseTypes.form.sectionBasics' | translate }}</h2></div>
        <div class="panel-body grid gap-x-5 gap-y-4 sm:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium" for="code">{{ 'caseTypes.form.code' | translate }} <span class="text-error">*</span></label>
            <input class="input ref w-full" type="text" id="code" name="caseTypeCode" maxlength="128" required
                   [placeholder]="'caseTypes.form.codePlaceholder' | translate" [(ngModel)]="model.code" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="title">{{ 'caseTypes.form.titleLabel' | translate }} <span class="text-error">*</span></label>
            <input class="input w-full" type="text" id="title" name="caseTypeTitle" maxlength="128" required
                   [placeholder]="'caseTypes.form.titlePlaceholder' | translate" [(ngModel)]="model.title" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="order">{{ 'caseTypes.form.order' | translate }}</label>
            <input class="input w-full kbd-ref" type="number" id="order" name="caseTypeOrder" [(ngModel)]="model.order" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="description">{{ 'caseTypes.form.description' | translate }}</label>
            <input class="input w-full" type="text" id="description" name="caseTypeDescription"
                   [placeholder]="'caseTypes.form.descriptionPlaceholder' | translate" [(ngModel)]="model.description" />
          </div>
        </div>
      </section>

      <section class="panel" aria-labelledby="ct-behaviour">
        <div class="panel-head"><h2 id="ct-behaviour">{{ 'caseTypes.form.sectionBehaviour' | translate }}</h2></div>
        <div class="panel-body grid gap-x-5 gap-y-4 sm:grid-cols-2">
          <div>
            <label class="mb-2 block text-sm font-medium" for="tags">{{ 'caseTypes.form.tags' | translate }}</label>
            <input class="input w-full" type="text" id="tags" name="caseTypeTags" placeholder="tag1,tag2" [(ngModel)]="model.tags" />
          </div>
          <div>
            <label class="mb-2 block text-sm font-medium" for="canCreateRoles">{{ 'caseTypes.form.canCreateRoles' | translate }}</label>
            <input class="input w-full" type="text" id="canCreateRoles" name="caseTypeCanCreateRoles" placeholder="RoleA,RoleB" [(ngModel)]="model.canCreateRoles" />
          </div>
          <label class="flex cursor-pointer items-start gap-3 sm:col-span-2">
            <input type="checkbox" class="toggle toggle-primary mt-0.5" name="caseTypeIsMenuItem" [(ngModel)]="model.isMenuItem" />
            <span>
              <span class="block font-medium">{{ 'caseTypes.form.isMenuItem' | translate }}</span>
              <span class="block text-xs text-quiet">{{ 'caseTypes.form.isMenuItemHint' | translate }}</span>
            </span>
          </label>
          <ng-container *ngTemplateOutlet="json; context: { field: 'config', label: 'caseTypes.form.config', wide: true }" />
        </div>
      </section>

      <section class="panel" aria-labelledby="ct-definition">
        <div class="panel-head"><h2 id="ct-definition">{{ 'caseTypes.form.sectionDefinition' | translate }}</h2></div>
        <div class="panel-body grid gap-x-5 gap-y-4 xl:grid-cols-2">
          <ng-container *ngTemplateOutlet="json; context: { field: 'dataSchema', label: 'caseTypes.form.dataSchema', required: true }" />
          <ng-container *ngTemplateOutlet="json; context: { field: 'layout', label: 'caseTypes.form.layout' }" />
          <ng-container *ngTemplateOutlet="json; context: { field: 'translations', label: 'caseTypes.form.translations' }" />
          <ng-container *ngTemplateOutlet="json; context: { field: 'layoutTranslations', label: 'caseTypes.form.layoutTranslations' }" />
        </div>
      </section>

      <section class="panel" aria-labelledby="ct-list">
        <div class="panel-head"><h2 id="ct-list">{{ 'caseTypes.form.sectionCaseList' | translate }}</h2></div>
        <div class="panel-body grid gap-x-5 gap-y-4 xl:grid-cols-2">
          <ng-container *ngTemplateOutlet="json; context: { field: 'gridFilterConfig', label: 'caseTypes.form.gridFilterConfig' }" />
          <ng-container *ngTemplateOutlet="json; context: { field: 'gridColumnConfig', label: 'caseTypes.form.gridColumnConfig' }" />
        </div>
      </section>

      <div class="sticky bottom-0 z-10 -mx-1 flex items-center justify-end gap-3 border-t border-base-300 bg-base-100 px-1 py-3">
        @if (invalidJsonCount() > 0) {
          <p class="mr-auto flex items-center gap-1.5 text-error" role="status">
            <ng-icon name="lucideTriangleAlert" size="1rem" /> {{ 'caseTypes.form.fixJson' | translate }}
          </p>
        }
        <button class="btn btn-primary" type="submit" [disabled]="!canSave() || form.invalid">
          <ng-icon name="lucideSave" size="1rem" /> {{ 'common.save' | translate }}
        </button>
      </div>
    </form>

    <ng-template #json let-field="field" let-label="label" let-required="required" let-wide="wide">
      <div [class]="wide ? 'sm:col-span-2' : ''">
        <label class="mb-2 block text-sm font-medium" [attr.for]="field">
          {{ label | translate }} @if (required) { <span class="text-error">*</span> }
        </label>
        <textarea class="textarea w-full font-mono text-[0.8125rem] leading-relaxed" rows="8" spellcheck="false"
                  [id]="field" [name]="field" placeholder="{ }"
                  [class.textarea-error]="jsonInvalid(field)"
                  [attr.aria-invalid]="jsonInvalid(field) ? true : null"
                  [attr.aria-describedby]="jsonInvalid(field) ? field + '-error' : null"
                  [ngModel]="jsonValue(field)" (ngModelChange)="onJsonChange($any(field), $event)"></textarea>
        @if (jsonInvalid(field)) {
          <p class="mt-1 text-xs text-error" role="alert" [id]="field + '-error'">{{ 'caseTypes.form.invalidJson' | translate }}</p>
        }
      </div>
    </ng-template>
  `
})
export class CaseTypeFormComponent {
  /** The case type to edit; an empty object for a new one. */
  public readonly data = input<ICaseTypeRequest | null | undefined>();
  /** Emitted with the edited case type when the user saves. */
  public readonly submitted = output<ICaseTypeRequest>();

  protected model: ICaseTypeRequest = {};
  protected readonly jsonText = signal<Record<JsonField, string>>(this.texts({}));
  protected readonly jsonErrors = signal<Partial<Record<JsonField, boolean>>>({});
  protected readonly invalidJsonCount = computed(() => Object.values(this.jsonErrors()).filter(Boolean).length);
  private readonly hasSchema = signal(false);
  protected readonly canSave = computed(() => this.invalidJsonCount() === 0 && this.hasSchema());

  constructor() {
    effect(() => {
      const data = this.data() ?? {};
      this.model = { ...data };
      this.jsonText.set(this.texts(data));
      this.jsonErrors.set({});
      this.hasSchema.set(data.dataSchema !== undefined && data.dataSchema !== null);
    });
  }

  protected jsonValue(field: string): string {
    return this.jsonText()[field as JsonField];
  }

  protected jsonInvalid(field: string): boolean {
    return !!this.jsonErrors()[field as JsonField];
  }

  protected onJsonChange(field: JsonField, text: string): void {
    this.jsonText.update(texts => ({ ...texts, [field]: text }));
    if (!text?.trim()) {
      (this.model as any)[field] = undefined;
      this.jsonErrors.update(errors => ({ ...errors, [field]: false }));
    } else {
      try {
        (this.model as any)[field] = JSON.parse(text);
        this.jsonErrors.update(errors => ({ ...errors, [field]: false }));
      } catch {
        // Keep the last valid value; saving stays blocked until the text parses.
        this.jsonErrors.update(errors => ({ ...errors, [field]: true }));
      }
    }
    if (field === 'dataSchema') {
      this.hasSchema.set(this.model.dataSchema !== undefined && this.model.dataSchema !== null && !this.jsonErrors().dataSchema);
    }
  }

  protected submit(): void {
    if (!this.canSave() || !this.model.code || !this.model.title) {
      return;
    }
    this.submitted.emit({ ...this.model });
  }

  /** Same rendering as the `json` pipe the previous form used. */
  private texts(data: ICaseTypeRequest): Record<JsonField, string> {
    return JSON_FIELDS.reduce((texts, field) => {
      const value = (data as any)[field];
      texts[field] = value === undefined ? '' : JSON.stringify(value, null, 2);
      return texts;
    }, {} as Record<JsonField, string>);
  }
}
