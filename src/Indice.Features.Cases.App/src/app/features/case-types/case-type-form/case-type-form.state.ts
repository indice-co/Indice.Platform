import { computed, signal } from '@angular/core';
import { CaseType, CaseTypeTranslation, ICaseTypeRequest } from 'src/app/core/services/cases-api.service';

export type CaseTypeJsonField = 'dataSchema' | 'layout' | 'config' | 'gridFilterConfig' | 'gridColumnConfig';

export interface CaseTypeGeneralModel {
  code: string;
  title: string;
  order: string;
  description: string;
  tags: string;
  canCreateRoles: string;
  isMenuItem: boolean;
}

/** Title / description of the case type in one language. */
export interface CaseTypeTextTranslation {
  title: string;
  description: string;
}

const JSON_FIELDS: CaseTypeJsonField[] = ['dataSchema', 'layout', 'config', 'gridFilterConfig', 'gridColumnConfig'];

/**
 * The state of a case type being created or edited, shared by the form sections (general, definition, configuration).
 * JSON documents are edited as text and only parsed when read, so typing never throws; invalid JSON is flagged per field.
 */
export class CaseTypeFormState {
  public readonly general = signal<CaseTypeGeneralModel>(toGeneral(undefined));
  public readonly json = signal<Record<CaseTypeJsonField, string>>(toJson(undefined));
  /** Title / description per language, keyed by lower case language code. */
  public readonly translations = signal<Record<string, CaseTypeTextTranslation>>({});
  /** Form texts: original text → translation (the server's flat `layoutTranslations`). */
  public readonly layoutTranslations = signal<Record<string, string>>({});

  public readonly invalidJson = computed(() => {
    const json = this.json();
    return JSON_FIELDS.filter(field => json[field].trim() && tryParse(json[field]) === INVALID);
  });
  /** The parsed schema / layout, or undefined while empty or invalid. */
  public readonly schema = computed(() => parseOrUndefined(this.json().dataSchema));
  public readonly layout = computed(() => parseOrUndefined(this.json().layout));
  /** Languages whose description is translated but whose title is not; the server would show an empty title for them. */
  public readonly translationsMissingTitle = computed(() =>
    Object.entries(this.translations()).filter(([, t]) => !t.title.trim() && !!t.description.trim()).map(([lang]) => lang));

  public readonly generalValid = computed(() => !!this.general().code.trim() && !!this.general().title.trim());
  public readonly schemaValid = computed(() => !!this.json().dataSchema.trim() && !this.invalidJson().includes('dataSchema'));
  public readonly layoutValid = computed(() => !this.invalidJson().includes('layout'));
  public readonly translationsValid = computed(() => this.translationsMissingTitle().length === 0);
  public readonly definitionValid = computed(() => this.schemaValid() && this.layoutValid() && this.translationsValid());
  public readonly configurationValid = computed(() =>
    !this.invalidJson().some(field => field === 'config' || field === 'gridFilterConfig' || field === 'gridColumnConfig'));
  public readonly valid = computed(() => this.generalValid() && this.definitionValid() && this.configurationValid());

  public load(data: ICaseTypeRequest | CaseType | undefined): void {
    this.general.set(toGeneral(data));
    this.json.set(toJson(data));
    this.translations.set(toTranslations(data?.translations));
    this.layoutTranslations.set(Object.fromEntries(Object.entries<unknown>(parseOrUndefined(data?.layoutTranslations) ?? {})
      .map(([text, translation]) => [text, typeof translation === 'string' ? translation : `${translation ?? ''}`])));
  }

  public setGeneral<K extends keyof CaseTypeGeneralModel>(key: K, value: CaseTypeGeneralModel[K]): void {
    this.general.update(general => ({ ...general, [key]: value }));
  }

  public setJson(field: CaseTypeJsonField, value: string): void {
    this.json.update(json => ({ ...json, [field]: value }));
  }

  public setTranslation(lang: string, key: keyof CaseTypeTextTranslation, value: string): void {
    this.translations.update(translations => {
      const current = translations[lang] ?? { title: '', description: '' };
      return { ...translations, [lang]: { ...current, [key]: value } };
    });
  }

  public setLayoutTranslation(text: string, translation: string): void {
    this.layoutTranslations.update(translations => ({ ...translations, [text]: translation }));
  }

  public removeLayoutTranslations(texts: readonly string[]): void {
    this.layoutTranslations.update(translations => {
      const remaining = { ...translations };
      texts.forEach(text => delete remaining[text]);
      return remaining;
    });
  }

  /** The request to send. Empty translations are left out: the server would otherwise replace texts with an empty string. */
  public toRequest(): ICaseTypeRequest {
    const general = this.general();
    const json = (field: CaseTypeJsonField) => parseOrUndefined(this.json()[field]);
    const translations = Object.fromEntries(Object.entries(this.translations())
      .map(([lang, t]) => [lang, { title: t.title.trim(), description: t.description.trim() }] as const)
      .filter(([, t]) => t.title || t.description)
      .map(([lang, t]) => [lang, new CaseTypeTranslation({ title: t.title || undefined, description: t.description || undefined })]));
    const layoutTranslations = Object.fromEntries(Object.entries(this.layoutTranslations()).filter(([, value]) => value.trim()));
    return {
      code: general.code.trim(),
      title: general.title.trim(),
      order: general.order === '' ? undefined : +general.order,
      description: general.description || undefined,
      tags: general.tags || undefined,
      canCreateRoles: general.canCreateRoles || undefined,
      isMenuItem: general.isMenuItem,
      dataSchema: json('dataSchema'),
      layout: json('layout'),
      translations: Object.keys(translations).length ? translations : undefined,
      layoutTranslations: Object.keys(layoutTranslations).length ? layoutTranslations : undefined,
      config: json('config'),
      gridFilterConfig: json('gridFilterConfig'),
      gridColumnConfig: json('gridColumnConfig')
    };
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

/** Parses JSON text (objects pass through); undefined when empty or invalid. */
function parseOrUndefined(value: any): any {
  if (typeof value !== 'string') {
    return value ?? undefined;
  }
  if (!value.trim()) {
    return undefined;
  }
  const parsed = tryParse(value);
  return parsed === INVALID ? undefined : parsed;
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

function toGeneral(data: ICaseTypeRequest | CaseType | undefined): CaseTypeGeneralModel {
  const roles = data?.canCreateRoles;
  return {
    code: data?.code ?? '',
    title: data?.title ?? '',
    order: data?.order === undefined || data?.order === null ? '' : `${data.order}`,
    description: data?.description ?? '',
    tags: data?.tags ?? '',
    canCreateRoles: Array.isArray(roles) ? roles.join(',') : roles ?? '',
    isMenuItem: !!data?.isMenuItem
  };
}

function toJson(data: ICaseTypeRequest | CaseType | undefined): Record<CaseTypeJsonField, string> {
  return {
    dataSchema: toText(data?.dataSchema),
    layout: toText(data?.layout),
    config: toText(data?.config),
    gridFilterConfig: toText(data?.gridFilterConfig),
    gridColumnConfig: toText(data?.gridColumnConfig)
  };
}

/** Language keys are lower-cased (the server matches them case-insensitively); the first spelling of a language wins. */
function toTranslations(value: any): Record<string, CaseTypeTextTranslation> {
  const translations: Record<string, CaseTypeTextTranslation> = {};
  for (const [lang, t] of Object.entries<any>(parseOrUndefined(value) ?? {})) {
    const key = lang.toLowerCase();
    translations[key] ??= { title: t?.title ?? '', description: t?.description ?? '' };
  }
  return translations;
}
