import { Injectable } from '@angular/core';

import * as app from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    constructor() { }

    public log(message: any): void {
        if (!app.settings.production) {
            console.log(message);
        }
    }

    public info(message: any): void {
        if (!app.settings.production) {
            console.info(message);
        }
    }

    public warn(message: any): void {
        if (!app.settings.production) {
            console.warn(message);
        }
    }

    public error(message: any): void {
        if (!app.settings.production) {
            console.error(message);
        }
    }
}
