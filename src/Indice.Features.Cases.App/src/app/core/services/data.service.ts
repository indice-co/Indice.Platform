import { Observable, AsyncSubject, Subscription } from 'rxjs';
import { OnDestroy, Injectable } from '@angular/core';

@Injectable()
export abstract class DataService implements OnDestroy {
    private cachedData: any = {};
    private $subscriptions: { [key: string]: Subscription } | undefined = {};

    constructor() { }

    protected getDataFromCacheOrHttp<T>(key: string, clientMethod: Observable<T>, expiresAt?: number) {
        if (!this.cachedData[key]) {
            this.cachedData[key] = new AsyncSubject<T>();

            if (this.$subscriptions!.hasOwnProperty(key)) {
                this.$subscriptions![key].unsubscribe();
            }

            this.$subscriptions![key] = clientMethod.subscribe((result: T) => {
                if (this.cachedData[key]) {
                    this.cachedData[key].next(result);
                    this.cachedData[key].complete();
                }
            }, error => {
                this.removeDataFromCache(key);
            });
            if (expiresAt) {
                setTimeout(() => this.removeDataFromCache(key), expiresAt);
            }
        }
        return this.cachedData[key];
    }

    protected removeDataFromCache(key: string): void {
        if (this.cachedData[key]) {
            delete this.cachedData[key];
        }
        if (this.$subscriptions!.hasOwnProperty(key)) {
            this.$subscriptions![key].unsubscribe();
            delete this.$subscriptions![key];
        }
    }

    protected setDataCache<T>(data: any, key: string) {
        this.cachedData[key] = new AsyncSubject<T>();
        this.cachedData[key] = data;
    }

    protected getDataFromCache<T>(key: string) {
        return this.cachedData[key];
    }

    ngOnDestroy(): void {
        if (this.$subscriptions) {
            for (const key in this.$subscriptions) {
                if (this.$subscriptions.hasOwnProperty(key)) {
                    this.$subscriptions[key].unsubscribe();
                }
            }
            this.$subscriptions = undefined;
        }
    }
}
