import { Observable, AsyncSubject, Subscription } from 'rxjs';
import { OnDestroy, Injectable } from '@angular/core';

@Injectable()
export abstract class DataService implements OnDestroy {
    private cachedData: any = {};
    private $subscriptions: { [key: string]: Subscription } | undefined = {};

    constructor() { }

    protected getDataFromCacheOrHttp<T>(key: string, clientMethod: Observable<T>, expiresAt?: number) {
        if (!this.cachedData[key]) {
            // keep a local reference: an invalidation while the request is in flight must not leave existing subscribers hanging
            const subject = new AsyncSubject<T>();
            this.cachedData[key] = subject;

            if (this.$subscriptions!.hasOwnProperty(key)) {
                this.$subscriptions![key].unsubscribe();
            }

            this.$subscriptions![key] = clientMethod.subscribe((result: T) => {
                subject.next(result);
                subject.complete();
            }, error => {
                subject.error(error);
                if (this.cachedData[key] === subject) {
                    this.removeDataFromCache(key);
                }
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
