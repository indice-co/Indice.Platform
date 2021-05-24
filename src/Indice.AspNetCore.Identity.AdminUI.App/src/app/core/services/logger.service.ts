import { Injectable } from '@angular/core';

import * as app from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    public log(message: any, ...parameters: any[]): void {
        if (!app.settings.production) {
            if (parameters?.length > 0) {
                console.log(message, parameters);
            } else {
                console.log(message);
            }
        }
    }

    public warn(message: any, ...parameters: any[]): void {
        if (!app.settings.production) {
            if (parameters?.length > 0) {
                console.warn(message, parameters);
            } else {
                console.warn(message);
            }
        }
    }
}
