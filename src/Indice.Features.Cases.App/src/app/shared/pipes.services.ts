import { Pipe, PipeTransform } from '@angular/core';

/**
 * Resolves a list column value from an item. When the column has an `itemProperty` (a dotted path) it is followed,
 * otherwise the camel cased column title is used as the property name. Dates are formatted, missing values become '-'.
 */
@Pipe({ name: 'valueFromPath' })
export class ValueFromPathPipe implements PipeTransform {
  transform(item: any, column: { title: string; itemProperty?: string }): any {
    const value = column.itemProperty
      ? this.getValueFromPropertyPath(item, column.itemProperty)
      : item?.[`${column.title[0].toLowerCase()}${column.title.slice(1)}`];
    if (value instanceof Date) {
      const stringifiedDate = value.toLocaleString('en-GB');
      return stringifiedDate.substring(0, stringifiedDate.length - 3);
    }
    return value === undefined || value === null ? '-' : value;
  }

  private getValueFromPropertyPath(givenObject: any, propertyPath: string): any {
    for (const prop of propertyPath.split('.')) {
      givenObject = givenObject?.[prop];
      if (givenObject === undefined) {
        return givenObject;
      }
    }
    return givenObject;
  }
}
