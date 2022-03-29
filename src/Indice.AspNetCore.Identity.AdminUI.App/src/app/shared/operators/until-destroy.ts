import { MonoTypeOperatorFunction, Observable } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

export const destroy$ = Symbol('destroy$');

export const untilDestroy = <T>(component: any): MonoTypeOperatorFunction<T> => {
    if (component[destroy$] === undefined) {
        addDestroyObservableToComponent(component);
    }
    return takeUntil<T>(component[destroy$]);
};

export function addDestroyObservableToComponent(component: any) {
    component[destroy$] = new Observable<void>(observer => {
        const originalDestroy = component.ngOnDestroy;
        if (originalDestroy == null) {
            throw new Error('untilDestroy operator needs the component to have an ngOnDestroy method.');
        }
        component.ngOnDestroy = () => {
            observer.next();
            observer.complete();
            originalDestroy.call(component);
        };
        return () => (component[destroy$] = undefined);
    });
}
