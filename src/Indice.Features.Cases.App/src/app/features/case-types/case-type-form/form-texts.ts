/**
 * Properties whose string values the server (`JsonTranslationService`) replaces with their layout translation, in both the
 * data schema and the layout. Its list also has `required` and `pattern`, which are left out here: they hold property names
 * and regular expressions, and translating them would break validation.
 */
export const TRANSLATABLE_PROPERTIES = ['title', 'placeholder', 'enumNames', 'helpvalue'] as const;

export type FormTextSource = 'schema' | 'layout';

/** A text of the schema or layout that the server translates. */
export interface FormText {
  text: string;
  source: FormTextSource;
  /** Where the text appears, e.g. `properties.firstName.title`. */
  paths: string[];
}

/** The translatable texts of the schema and the layout, in document order, each text once. */
export function collectFormTexts(schema: unknown, layout: unknown): FormText[] {
  const texts = new Map<string, FormText>();
  const add = (source: FormTextSource) => (text: string, path: string) => {
    const existing = texts.get(text);
    if (existing) {
      existing.paths.push(path);
    } else {
      texts.set(text, { text, source, paths: [path] });
    }
  };
  walk(schema, '', undefined, (property, text, path) => isTranslatable(property) && add('schema')(text, path));
  walk(layout, '', undefined, (property, text, path) => isTranslatable(property) && add('layout')(text, path));
  return [...texts.values()];
}

/** The `label` texts of a JSON Forms UI schema layout. The server does not translate `label`, so these stay as written. */
export function collectLabels(layout: unknown): string[] {
  const labels = new Set<string>();
  walk(layout, '', undefined, (property, text) => property === 'label' && labels.add(text));
  return [...labels];
}

function isTranslatable(property: string | undefined): boolean {
  return (TRANSLATABLE_PROPERTIES as readonly string[]).includes(property ?? '');
}

/** Visits every non-empty string; array items report the property that holds the array (as the server does for `enumNames`). */
function walk(node: unknown, path: string, property: string | undefined, visit: (property: string | undefined, text: string, path: string) => void): void {
  if (typeof node === 'string') {
    if (node.trim()) {
      visit(property, node, path);
    }
  } else if (Array.isArray(node)) {
    node.forEach((item, index) => walk(item, `${path}[${index}]`, property, visit));
  } else if (node && typeof node === 'object') {
    for (const [key, value] of Object.entries(node)) {
      walk(value, path ? `${path}.${key}` : key, key, visit);
    }
  }
}
