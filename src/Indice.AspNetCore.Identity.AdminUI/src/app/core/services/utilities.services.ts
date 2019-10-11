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

    public minutesToSeconds(minutes: number): number {
        if (minutes <= 0) {
            throw new Error(`Parameter ${minutes} must be a non-negative value.`);
        }
        return minutes * 60;
    }

    public hoursToSeconds(hours: number): number {
        if (hours <= 0) {
            throw new Error(`Parameter ${hours} must be a non-negative value.`);
        }
        return this.minutesToSeconds(hours * 60);
    }

    public daysToSeconds(days: number): number {
        if (days <= 0) {
            throw new Error(`Parameter ${days} must be a non-negative value.`);
        }
        return this.hoursToSeconds(days * 24);
    }

    public weeksToSeconds(weeks: number): number {
        if (weeks <= 0) {
            throw new Error(`Parameter ${weeks} must be a non-negative value.`);
        }
        return this.daysToSeconds(weeks * 7);
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

    public secondsToText(totalSeconds: number): string {
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds - (hours * 3600)) / 60);
        const seconds = totalSeconds - (hours * 3600) - (minutes * 60);
        return `
            ${hours > 0 ? `${hours < 10 ? `0${hours}` : hours} hr${minutes > 0 || seconds > 0 ? ',' : ''}` : ''}
            ${minutes > 0 ? `${minutes < 10 ? `0${minutes}` : minutes} min${seconds > 0 ? ',' : ''}` : ''}
            ${seconds > 0 ? `${seconds < 10 ? `0${seconds}` : seconds} secs` : ''}
        `;
    }
}
