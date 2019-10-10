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

    public padNumber(value: number): string {
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

    public weeksToSeconds(weeks: number): number {
        if (weeks <= 0) {
            throw new Error(`Parameter ${weeks} must be a non-negative value.`);
        }
        return weeks * 604800;
    }

    public monthsToSeconds(months: number): number {
        if (months <= 0) {
            throw new Error(`Parameter ${months} must be a non-negative value.`);
        }
        return this.weeksToSeconds(months * 4);
    }

    public yearsToSeconds(years: number): number {
        if (years <= 0) {
            throw new Error(`Parameter ${years} must be a non-negative value.`);
        }
        return this.monthsToSeconds(years * 12);
    }
}
