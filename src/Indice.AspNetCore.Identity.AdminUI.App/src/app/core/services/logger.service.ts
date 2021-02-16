import { Injectable } from '@angular/core';

import * as settings from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    public log(message: any): void {
        if (!settings.getAppSettings().production) {
            console.log(message);
        }
    }

    public warn(message: any): void {
        if (!settings.getAppSettings().production) {
            console.warn(message);
        }
    }
}
