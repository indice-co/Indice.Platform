import { Injectable } from '@angular/core';

import * as Oidc from 'oidc-client';
import * as app from '../models/settings';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    constructor() {
        if (!app.settings.production) {
            Oidc.Log.logger = console;
        }
    }

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
