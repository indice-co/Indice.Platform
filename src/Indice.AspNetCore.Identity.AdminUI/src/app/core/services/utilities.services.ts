import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class UtilitiesService {
    public toInteger(value: any): number {
        return parseInt(`${value}`, 10);
    }

    public isNumber(value: any): value is number {
        return !isNaN(this.toInteger(value));
    }

    public padNumber(value: number) {
        return this.isNumber(value) ? `0${value}`.slice(-2) : '';
    }

    public newGuid() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
            // tslint:disable-next-line: no-bitwise
            const random = Math.random() * 16 | 0;
            // tslint:disable-next-line: no-bitwise
            const value = char === 'x' ? random : (random & 0x3 | 0x8);
            return value.toString(16);
        });
    }
}
