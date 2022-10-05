import { Pipe, PipeTransform } from "@angular/core";

@Pipe({ name: 'beautifyBoolean' })
export class BeautifyBooleanPipe implements PipeTransform {
    transform(value: boolean | undefined): string {
        return value ? '<span class="ms-Icon ms-Icon--StatusCircleCheckmark text-green-500 text-3xl"></span>' : '<span class="ms-Icon ms-Icon--StatusCircleBlock2 text-red-500 text-3xl"></span>';
    }
}

@Pipe({ name: 'removePrefix' })
export class RemovePrefixPipe implements PipeTransform {
    transform(value: string | undefined): string {
        return value ? value.split(":")[1] : '';
    }
}