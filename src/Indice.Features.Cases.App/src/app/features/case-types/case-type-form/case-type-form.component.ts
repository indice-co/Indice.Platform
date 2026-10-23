import { Component, computed, effect, input, output, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideSave } from '@lucide/angular';
import { CaseType, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';
import { JsonEditorComponent } from 'src/app/shared/components/json-editor/json-editor.component';

type JsonField = 'dataSchema' | 'layout' | 'translations' | 'layoutTranslations' | 'config' | 'gridFilterConfig' | 'gridColumnConfig';

interface CaseTypeFormModel {
  code: string;
  title: string;
  order: string;
  description: string;
  tags: string;
  canCreateRoles: string;
  isMenuItem: boolean;
  json: Record<JsonField, string>;
}

const JSON_FIELDS: JsonField[] = ['dataSchema', 'layout', 'translations', 'layoutTranslations', 'config', 'gridFilterConfig', 'gridColumnConfig'];

/**
 * Case type editor. JSON documents are edited as text and only parsed on save, so typing never throws;
 * invalid JSON is flagged per field and blocks saving.
 */
@Component({
  selector: 'app-case-type-form',
  imports: [NgTemplateOutlet, RouterLink, TranslatePipe, LucideSave, JsonEditorComponent],
  template: `
    <form class="space-y-6 pb-4" novalidate autocomplete="off" (submit)="$event.preventDefault(); submit()">
      <section class="panel p-5 sm:p-6">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 md:grid-cols-2">
          <legend class="fieldset-legend text-base">{{ 'caseTypes.form.legend' | translate }}</legend>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.code' | translate }} <span class="text-error">*</span></span>
            <input class="input w-full font-mono" type="text" maxlength="128" required
              [placeholder]="'caseTypes.form.codePlaceholder' | translate" [value]="form().code" (input)="set('code', value($event))" />
          </label>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.titleLabel' | translate }} <span class="text-error">*</span></span>
            <input class="input w-full" type="text" maxlength="128" required
              [placeholder]="'caseTypes.form.titlePlaceholder' | translate" [value]="form().title" (input)="set('title', value($event))" />
          </label>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.order' | translate }}</span>
            <input class="input w-full" type="number" [value]="form().order" (input)="set('order', value($event))" />
          </label>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.description' | translate }}</span>
            <input class="input w-full" type="text"
              [placeholder]="'caseTypes.form.descriptionPlaceholder' | translate" [value]="form().description" (input)="set('description', value($event))" />
          </label>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.tags' | translate }}</span>
            <input class="input w-full" type="text" placeholder="tag1,tag2" [value]="form().tags" (input)="set('tags', value($event))" />
          </label>
          <label class="w-full">
            <span class="label mb-1">{{ 'caseTypes.form.canCreateRoles' | translate }}</span>
            <input class="input w-full font-mono" type="text" placeholder="RoleA,RoleB" [value]="form().canCreateRoles" (input)="set('canCreateRoles', value($event))" />
          </label>
          <label class="label mt-2 cursor-pointer gap-3 text-base-content md:col-span-2">
            <input type="checkbox" class="toggle toggle-primary toggle-sm" [checked]="form().isMenuItem" (change)="set('isMenuItem', checked($event))" />
            <span>
              <span class="block font-medium">{{ 'caseTypes.form.isMenuItem' | translate }}</span>
              <span class="block text-xs text-base-content/60">{{ 'caseTypes.form.isMenuItemHint' | translate }}</span>
            </span>
          </label>
        </fieldset>
      </section>

      <section class="panel p-5 sm:p-6">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 lg:grid-cols-2">
          <legend class="fieldset-legend text-base">{{ 'caseTypes.form.sectionData' | translate }}</legend>
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'dataSchema', label: 'caseTypes.form.schema', required: true, rows: 12 }" />
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'layout', label: 'caseTypes.form.layout', rows: 12 }" />
        </fieldset>
      </section>

      <section class="panel p-5 sm:p-6">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 lg:grid-cols-2">
          <legend class="fieldset-legend text-base">{{ 'caseTypes.form.sectionTranslations' | translate }}</legend>
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'translations', label: 'caseTypes.form.translations', rows: 8 }" />
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'layoutTranslations', label: 'caseTypes.form.layoutTranslations', rows: 8 }" />
        </fieldset>
      </section>

      <section class="panel p-5 sm:p-6">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 lg:grid-cols-2">
          <legend class="fieldset-legend text-base">{{ 'caseTypes.form.sectionConfig' | translate }}</legend>
          <div class="lg:col-span-2">
            <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'config', label: 'caseTypes.form.config', rows: 8 }" />
          </div>
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'gridFilterConfig', label: 'caseTypes.form.gridFilterConfig', rows: 6 }" />
          <ng-container *ngTemplateOutlet="jsonEditor; context: { field: 'gridColumnConfig', label: 'caseTypes.form.gridColumnConfig', rows: 6 }" />
        </fieldset>
      </section>

      <div class="sticky bottom-0 z-10 -mx-4 flex items-center justify-end gap-2 border-t border-base-300 bg-base-200/90 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8">
        @if (invalidJson().length) {
          <p class="mr-auto text-sm text-error">{{ 'caseTypes.form.fixJson' | translate }}</p>
        }
        <a routerLink="/case-types" class="btn btn-ghost">{{ 'common.cancel' | translate }}</a>
        <button type="submit" class="btn btn-primary" [disabled]="!canSave()">
          @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
          {{ 'common.save' | translate }}
        </button>
      </div>
    </form>

    <ng-template #jsonEditor let-field="field" let-label="label" let-required="required" let-rows="rows">
      <div class="w-full">
        <span class="label mb-1" [id]="'case-type-' + field">{{ label | translate }} @if (required) { <span class="text-error">*</span> }</span>
        <app-json-editor [rows]="rows" [invalid]="invalidJson().includes(field)" [labelledBy]="'case-type-' + field"
          [value]="jsonText(field)" (valueChange)="setJson(field, $event)" />
        @if (invalidJson().includes(field)) {
          <span class="mt-1 block text-xs text-error">{{ 'caseTypes.form.invalidJson' | translate }}</span>
        }
      </div>
    </ng-template>
  `
})
export class CaseTypeFormComponent {
  /** Initial values (an existing case type, or empty for create). */
  public readonly data = input<ICaseTypeRequest | CaseType | undefined>(undefined);
  public readonly busy = input(false);
  public readonly submitted = output<ICaseTypeRequest>();

  protected readonly form = signal<CaseTypeFormModel>(toModel(undefined));

  protected readonly invalidJson = computed(() => {
    const json = this.form().json;
    return JSON_FIELDS.filter(field => json[field].trim() && tryParse(json[field]) === INVALID);
  });

  protected readonly canSave = computed(() => {
    const form = this.form();
    return !this.busy() && !!form.code.trim() && !!form.title.trim() && !!form.json.dataSchema.trim() && this.invalidJson().length === 0;
  });

  constructor() {
    effect(() => this.form.set(toModel(this.data())));
  }

  protected set<K extends keyof Omit<CaseTypeFormModel, 'json'>>(key: K, value: CaseTypeFormModel[K]): void {
    this.form.update(form => ({ ...form, [key]: value }));
  }

  protected setJson(field: JsonField, value: string): void {
    this.form.update(form => ({ ...form, json: { ...form.json, [field]: value } }));
  }

  protected jsonText(field: JsonField): string {
    return this.form().json[field];
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected checked(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }

  protected submit(): void {
    if (!this.canSave()) {
      return;
    }
    const form = this.form();
    const json = (field: JsonField) => form.json[field].trim() ? tryParse(form.json[field]) : undefined;
    this.submitted.emit({
      code: form.code.trim(),
      title: form.title.trim(),
      order: form.order === '' ? undefined : +form.order,
      description: form.description || undefined,
      tags: form.tags || undefined,
      canCreateRoles: form.canCreateRoles || undefined,
      isMenuItem: form.isMenuItem,
      dataSchema: json('dataSchema'),
      layout: json('layout'),
      translations: json('translations'),
      layoutTranslations: json('layoutTranslations'),
      config: json('config'),
      gridFilterConfig: json('gridFilterConfig'),
      gridColumnConfig: json('gridColumnConfig')
    });
  }
}

const INVALID = Symbol('invalid');

function tryParse(text: string): any {
  try {
    return JSON.parse(text);
  } catch {
    return INVALID;
  }
}

function toText(value: any): string {
  if (value === undefined || value === null || value === '') {
    return '';
  }
  if (typeof value === 'string') {
    // stored as a JSON string: pretty print it when it parses
    const parsed = tryParse(value);
    return parsed === INVALID ? value : JSON.stringify(parsed, null, 2);
  }
  return JSON.stringify(value, null, 2);
}

function toModel(data: ICaseTypeRequest | CaseType | undefined): CaseTypeFormModel {
  const roles = data?.canCreateRoles;
  return {
    code: data?.code ?? '',
    title: data?.title ?? '',
    order: data?.order === undefined || data?.order === null ? '' : `${data.order}`,
    description: data?.description ?? '',
    tags: data?.tags ?? '',
    canCreateRoles: Array.isArray(roles) ? roles.join(',') : roles ?? '',
    isMenuItem: !!data?.isMenuItem,
    json: {
      dataSchema: toText(data?.dataSchema),
      layout: toText(data?.layout),
      translations: toText(data?.translations),
      layoutTranslations: toText(data?.layoutTranslations),
      config: toText(data?.config),
      gridFilterConfig: toText(data?.gridFilterConfig),
      gridColumnConfig: toText(data?.gridColumnConfig)
    }
  };
}
