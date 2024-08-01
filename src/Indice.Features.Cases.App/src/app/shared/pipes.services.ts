import { Pipe, PipeTransform } from "@angular/core";

@Pipe({ name: 'beautifyBoolean' })
export class BeautifyBooleanPipe implements PipeTransform {
  transform(value: boolean | undefined): string {
    return value ? '<span class="ms-Icon ms-Icon--StatusCircleCheckmark text-green-500 text-3xl"></span>' : '<span class="ms-Icon ms-Icon--StatusCircleBlock2 text-red-500 text-3xl"></span>';
  }
}

@Pipe({
  name: 'valueFromPath',
  pure: true
})
export class ValueFromPathPipe implements PipeTransform {
  constructor() { }

  transform(item: any, column: any): any {
    //if column has an "itemProperty" then get its value, else the value is the title in camel case
    const value = column.itemProperty ? this.getValueFromPropertyPath(item, column.itemProperty) : item[`${column.title[0].toLowerCase()}${column.title.slice(1)}`];
    let formattedValue = value;
    if (value instanceof Date) {
      const stringifiedDate = value.toLocaleString('en-GB');
      formattedValue = stringifiedDate.substring(0, stringifiedDate.length - 3);
    }
    if (value === undefined || value === null) {
      formattedValue = '-';
    }
    return formattedValue;
  }

  //this method navigates through the "dots" of the property path and finds the corresponding value of the object
  private getValueFromPropertyPath(givenObject: any, propertyPath: any) {
    const props = propertyPath.split('.');
    for (const prop of props) {
      givenObject = givenObject[prop];
      if (givenObject === undefined) {
        return givenObject;
      }
    }
    return givenObject;
  }
}
