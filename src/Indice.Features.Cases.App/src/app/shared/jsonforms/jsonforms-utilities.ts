import { JsonFormsAngularConfig } from '@jsonforms/angular';
import { getErrorAt, JsonSchema, UISchemaElement } from '@jsonforms/core';

/** The AJV error type used by JSON Forms (its own AJV version, not the one of ajsf). */
export type ErrorObject = ReturnType<ReturnType<typeof getErrorAt>>[number];

/** Values the case form host passes to the renderers through the `config` input of `<jsonforms>`. */
export interface CasesJsonFormsConfig extends JsonFormsAngularConfig {
    /** The case is a draft (changes the submit button text). */
    draft?: boolean;
    /** Called by the `Submit` element with the current form data. */
    onSubmit?: (data: any) => void;
}

/** What a renderer needs to draw a field the way the ajsf Tailwind framework did. */
export interface FieldPresentation {
    /** Schema keywords merged with the UI schema options (the ajsf `options` object). */
    options: any;
    /** Label HTML, with the required asterisk. `null` when the element has no title. */
    title: string | null;
    notitle: boolean;
    readonly: boolean;
    shown: boolean;
}

// Schema keywords that ajsf did not copy into a widget's options.
const excludedSchemaKeywords = ['additionalProperties', 'additionalItems', 'properties', 'items', 'required', 'type', 'x-schema-form', '$ref'];

/** Builds the ajsf-like options and visibility of a field. */
export function presentField(
    uischema: UISchemaElement & { label?: string | boolean | object },
    schema: JsonSchema | undefined,
    path: string | undefined,
    rootData: any,
    state: { visible: boolean, enabled: boolean, required?: boolean },
    caseChannel: string | undefined,
    addAsterisk = true
): FieldPresentation {
    const options: any = {};
    for (const [key, value] of Object.entries(schema ?? {})) {
        if (!excludedSchemaKeywords.includes(key)) {
            options[key] = value;
        }
    }
    Object.assign(options, uischema.options ?? {});
    if (state.required) {
        options.required = true;
    }
    const notitle = uischema.label === false || isTrue(options.notitle);
    let title: string | null = typeof uischema.label === 'string' ? uischema.label : options.title ?? fixTitle(lastSegment(scopeOf(uischema) ?? ''));
    if (title && addAsterisk && !notitle && options.required && !title.includes('*')) {
        title += ' <span class="text-error" aria-hidden="true">*</span>';
    }
    return {
        options,
        title: title || null,
        notitle,
        readonly: !state.enabled || isTrue(options.readonly),
        shown: state.visible && isShownForChannel(options, caseChannel) && evaluateCondition(options.condition, rootData, arrayIndicesOf(path)),
    };
}

/** ajsf `case-channel` option: the field is only shown for cases of that channel (e.g. 'Managed' or 'External'). */
export function isShownForChannel(options: any, caseChannel: string | undefined): boolean {
    return !options?.['case-channel'] || normalizeChannel(options['case-channel']) === normalizeChannel(caseChannel);
}

/** Legacy channel names, still found on older cases and UI schemas, mapped to their current names. */
const legacyChannels: Record<string, string> = { Agent: 'Managed', Customer: 'External' };

function normalizeChannel(channel: string | undefined): string | undefined {
    return channel && Object.hasOwn(legacyChannels, channel) ? legacyChannels[channel] : channel;
}

/** ajsf layouts store booleans as `true` or `"true"`. */
export function isTrue(value: any): boolean {
    return value === true || value === 'true';
}

/** Joins css classes, skipping empty ones. */
export function addClasses(...classes: (string | null | undefined | false)[]): string {
    return classes.filter(c => !!c).join(' ');
}

/** The array indices in a JSON Forms data path (`phones.0.number` -> `[0]`), like ajsf's `dataIndex`. */
export function arrayIndicesOf(path: string | undefined): number[] {
    return (path ?? '').split('.').filter(s => /^\d+$/.test(s)).map(Number);
}

/**
 * Evaluates an ajsf layout `condition` exactly like ajsf (`JsonSchemaFormService.evaluateCondition`):
 * - a string is an object path into the form data (`a.b`, `a[0].b`, `/a/b`), where a segment may be an
 *   expression `name==value` or `name!=value`; the condition holds when the path resolves to a truthy value;
 * - `{ functionBody }` is executed as `function (model, arrayIndices) { <functionBody> }`.
 */
export function evaluateCondition(condition: any, data: any, arrayIndices: number[] = []): boolean {
    if (condition === undefined || condition === null) {
        return true;
    }
    if (typeof condition === 'string') {
        let pointer = condition;
        if (arrayIndices.length) {
            pointer = pointer.replace('[arrayIndex]', `[${arrayIndices[arrayIndices.length - 1]}]`);
        }
        const keys = parseObjectPath(pointer);
        let result = !!getByPath(data, keys);
        if (!result && keys[0] === 'model') {
            result = !!getByPath({ model: data }, keys);
        }
        return result;
    }
    if (typeof condition === 'function') {
        return condition(data);
    }
    if (typeof condition.functionBody === 'string') {
        try {
            return new Function('model', 'arrayIndices', condition.functionBody)(data, arrayIndices);
        } catch {
            console.error('condition functionBody errored out on evaluation: ' + condition.functionBody);
            return true;
        }
    }
    return true;
}

function getByPath(data: any, keys: string[]): any {
    if (data === null || typeof data !== 'object') {
        return undefined;
    }
    let current = data;
    for (let key of keys) {
        if (key === '-' && Array.isArray(current) && current.length) {
            key = `${current.length - 1}`;
        }
        if (current !== null && typeof current === 'object' && Object.prototype.hasOwnProperty.call(current, key)) {
            current = current[key];
            continue;
        }
        const expression = evaluateExpression(current, key);
        if (!expression.passed) {
            return undefined;
        }
        current = expression.key ? current[expression.key] : current;
    }
    return current;
}

function evaluateExpression(current: any, key: string): { passed: boolean, key: string | null } {
    if (!key || !current) {
        return { passed: false, key };
    }
    const operator = key.includes('==') ? '==' : key.includes('!=') ? '!=' : null;
    if (!operator) {
        return { passed: false, key };
    }
    const [name, rawValue] = key.split(operator, 2);
    if (!name || !rawValue) {
        return { passed: false, key };
    }
    if (!Object.prototype.hasOwnProperty.call(current, name)) {
        return { passed: operator === '!=', key: null };
    }
    const value = rawValue.startsWith('\'') && rawValue.endsWith('\'') ? rawValue.replace('\'', '').replace('\'', '') : rawValue;
    const passed = operator === '==' ? current[name] === value : current[name] !== value;
    return passed ? { passed: true, key: name } : { passed: false, key };
}

/** Port of ajsf `JsonPointer.parseObjectPath` (plus JSON pointers). */
function parseObjectPath(path: string): string[] {
    if (path === '' || path === '#') {
        return [];
    }
    if (path[0] === '/' || path.startsWith('#/')) {
        return path.replace(/^#?\//, '').split('/').map(k => k.replace(/~1/g, '/').replace(/~0/g, '~'));
    }
    let index = 0;
    const parts: string[] = [];
    while (index < path.length) {
        const nextDot = path.indexOf('.', index);
        const nextOB = path.indexOf('[', index);
        if (nextDot === -1 && nextOB === -1) {
            parts.push(path.slice(index));
            index = path.length;
        } else if (nextDot !== -1 && (nextDot < nextOB || nextOB === -1)) {
            parts.push(path.slice(index, nextDot));
            index = nextDot + 1;
        } else {
            if (nextOB > index) {
                parts.push(path.slice(index, nextOB));
                index = nextOB;
            }
            const quote = path.charAt(nextOB + 1);
            if (quote === '"' || quote === '\'') {
                let nextCB = path.indexOf(quote + ']', nextOB);
                while (nextCB !== -1 && path.charAt(nextCB - 1) === '\\') {
                    nextCB = path.indexOf(quote + ']', nextCB + 2);
                }
                if (nextCB === -1) {
                    nextCB = path.length;
                }
                parts.push(path.slice(index + 2, nextCB).replace(new RegExp('\\' + quote, 'g'), quote));
                index = nextCB + 2;
            } else {
                let nextCB = path.indexOf(']', nextOB);
                if (nextCB === -1) {
                    nextCB = path.length;
                }
                parts.push(path.slice(index + 1, nextCB));
                index = nextCB + 1;
            }
            if (path.charAt(index) === '.') {
                index++;
            }
        }
    }
    return parts;
}

/** Select options from `titleMap` / `enumNames` and `enum` (ajsf `buildTitleMap`, flat list) or from a `oneOf` of constants. */
export function buildTitleMap(options: any): { name: string, value: any }[] {
    const titleMap = options.titleMap ?? options.enumNames;
    const enumList: any[] | undefined = options.enum;
    if (Array.isArray(titleMap)) {
        if (!enumList) {
            return titleMap;
        }
        return titleMap.every(t => typeof t === 'object')
            ? titleMap.filter(t => enumList.includes(t.value))
            : enumList.map((value, i) => ({ name: titleMap[i] ?? value, value }));
    }
    if (titleMap && typeof titleMap === 'object') {
        return enumList
            ? enumList.filter(value => Object.prototype.hasOwnProperty.call(titleMap, value)).map(value => ({ name: titleMap[value], value }))
            : Object.keys(titleMap).map(value => ({ name: titleMap[value], value }));
    }
    if (enumList) {
        return enumList.map(value => ({ name: `${value}`, value }));
    }
    if (Array.isArray(options.oneOf)) {
        return options.oneOf
            .filter((o: any) => o && ('const' in o || (Array.isArray(o.enum) && o.enum.length === 1)))
            .map((o: any) => {
                const value = 'const' in o ? o.const : o.enum[0];
                return { name: o.title ?? `${value}`, value };
            });
    }
    return [];
}

/** ajsf `formatErrors`: the validation messages of the layout, keyed by AJV keyword. */
export function formatErrors(errors: ErrorObject[], validationMessages: any): string | null {
    if (!errors.length) {
        return null;
    }
    const keywords = [...new Set(errors.map(e => e.keyword))];
    return keywords
        // Hide 'required' error, unless it is the only one.
        .filter(keyword => keyword !== 'required' || keywords.length === 1)
        .map(keyword => {
            const error = errors.find(e => e.keyword === keyword)!;
            if (typeof validationMessages === 'string') {
                return validationMessages;
            }
            const message = validationMessages?.[keyword];
            if (typeof message === 'string') {
                return message.replace(/{{(.+?)}}/g, (match, property) => (error.params as any)?.[property] ?? match);
            }
            return error.message ?? keyword;
        })
        .join('<br>');
}

/** A number as an amount in the Greek format (`1.234,56`), as the ajsf `currency` widget shows it. */
export function formatCurrency(value: number | string | undefined | null, decimalPlaces = 2, locale = 'el-GR'): string {
    if (typeof value === 'string') {
        value = value.trim() === '' || isNaN(Number(value)) ? value as any : Number(value);
    }
    return value?.toLocaleString(locale, {
        minimumFractionDigits: decimalPlaces,
        maximumFractionDigits: decimalPlaces
    }) || '';
}

/** A value counts as empty when it is missing, an empty string or an empty array. */
export function isEmptyValue(value: any): boolean {
    return value === undefined || value === null || value === '' || (Array.isArray(value) && value.length === 0);
}

/** ajsf child sizing in a flex container: `options.flex` ("grow shrink basis") or the separate options, default `1 1 auto`. */
export function flexItemStyle(options: any): { grow: string, shrink: string, basis: string, alignSelf: string | null, order: string | null } {
    const parts = (options?.flex ?? '').split(/\s+/);
    return {
        grow: parts[0] || options?.['flex-grow'] || '1',
        shrink: parts[1] || options?.['flex-shrink'] || '1',
        basis: parts[2] || options?.['flex-basis'] || 'auto',
        alignSelf: options?.['align-self'] ?? null,
        order: options?.order ?? null,
    };
}

function scopeOf(uischema: any): string | undefined {
    return typeof uischema?.scope === 'string' ? uischema.scope : undefined;
}

function lastSegment(scope: string): string {
    const segments = scope.split('/').filter(s => s && s !== '#' && s !== 'properties' && s !== 'items');
    return segments[segments.length - 1] ?? '';
}

/** ajsf `fixTitle`: a title from a property name (`homePhone` -> `Home Phone`). */
export function fixTitle(name: string): string {
    return name
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/_/g, ' ')
        .replace(/(^|\s)(\S)/g, (_, space, letter) => space + letter.toUpperCase());
}
