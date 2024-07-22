import { Pipe, PipeTransform } from "@angular/core";
import { DatePipe } from "@angular/common";

@Pipe({ name: 'beautifyBoolean' })
export class BeautifyBooleanPipe implements PipeTransform {
  transform(value: boolean | undefined): string {
    return value ? '<span class="ms-Icon ms-Icon--StatusCircleCheckmark text-green-500 text-3xl"></span>' : '<span class="ms-Icon ms-Icon--StatusCircleBlock2 text-red-500 text-3xl"></span>';
  }
}

@Pipe({
  name: 'itemValue',
  pure: true
})
export class ItemValuePipe implements PipeTransform {
  constructor(private datePipe: DatePipe) { }

  transform(item: any, column: any): any {
    return this.getItemValue(item, column);
  }

  public getItemValue(item: any, column: any) {
    //if column has an "itemProperty" then get its value, else the value is the title in camel case
    const value = column.itemProperty ? this.getValueFromPropertyPath(item, column.itemProperty) : item[`${column.title[0].toLowerCase()}${column.title.slice(1)}`];
    let formattedValue = value;
    if (value instanceof Date) {
      // Format date using DatePipe
      formattedValue = this.datePipe.transform(value, 'dd/MM/yy, HH:mm') || '-';
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
