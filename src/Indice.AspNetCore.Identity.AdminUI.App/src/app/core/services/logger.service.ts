import { Injectable } from '@angular/core';

import * as app from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    public log(message: any): void {
        if (!app.settings.production) {
            console.log(message);
        }
    }

    public warn(message: any): void {
        if (!app.settings.production) {
            console.warn(message);
        }
    }
}
