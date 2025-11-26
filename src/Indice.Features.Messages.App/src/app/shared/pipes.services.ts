import { Pipe, PipeTransform } from "@angular/core";

@Pipe({
    name: 'beautifyBoolean',
    standalone: false
})
export class BeautifyBooleanPipe implements PipeTransform {
    transform(value: boolean | undefined): string {
      return value ? '<span class="ms-Icon ms-Icon--StatusCircleCheckmark list-checkmark"></span>' : '<span class="ms-Icon ms-Icon--StatusCircleBlock2 list-dash"></span>';
    }
}
