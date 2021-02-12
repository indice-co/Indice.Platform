import { Injectable } from '@angular/core';

import { environment } from 'src/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class LoggerService {
    public log(message: any): void {
        if (!environment.production) {
            console.log(message);
        }
    }

    public warn(message: any): void {
        if (!environment.production) {
            console.warn(message);
        }
    }
}
