import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'sum',
  standalone: false
})
export class SumPipe implements PipeTransform {
  transform(items: any[], attr: string| undefined | null): any {
    if (attr) {
      return items?.reduce((a, b) => a + b[attr], 0) || 0;
    }
    return items?.reduce((a, b) => a + b, 0) || 0;
  }
}
