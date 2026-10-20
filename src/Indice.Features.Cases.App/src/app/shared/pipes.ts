import { Pipe, PipeTransform } from '@angular/core';

/** A grid column: its title (also the translation key suffix) and, optionally, the path of the value in the item. */
export interface GridColumn {
  title: string;
  itemProperty?: string;
}

/**
 * Reads a column's value from a list item: `itemProperty` is a dotted path, otherwise the camel-cased title is the
 * property name. Dates are shown as `dd/MM/yyyy, HH:mm` and missing values as `-`.
 */
@Pipe({ name: 'valueFromPath' })
export class ValueFromPathPipe implements PipeTransform {
  public transform(item: any, column: GridColumn): any {
    const value = column.itemProperty
      ? valueAtPath(item, column.itemProperty)
      : item?.[`${column.title[0].toLowerCase()}${column.title.slice(1)}`];
    if (value instanceof Date) {
      const text = value.toLocaleString('en-GB');
      return text.substring(0, text.length - 3);
    }
    return value === undefined || value === null ? '-' : value;
  }
}

/** `en-GB` date and time, as the case lists always showed it. */
@Pipe({ name: 'toReadableDate' })
export class ToReadableDatePipe implements PipeTransform {
  public transform(value: any): string {
    return new Date(value).toLocaleString('en-GB');
  }
}

/** Follows a dotted property path (`caseType.title`); `undefined` as soon as a segment is missing. */
export function valueAtPath(item: any, path: string): any {
  let current = item;
  for (const prop of path.split('.')) {
    current = current?.[prop];
    if (current === undefined) {
      return undefined;
    }
  }
  return current;
}
