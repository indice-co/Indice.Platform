import { ApplicationRef, createComponent, EnvironmentInjector, inject, Injectable, InjectionToken, Injector, Type } from '@angular/core';
import { Observable, Subject } from 'rxjs';

/** Data passed to a dialog component through {@link DialogService.open}. */
export const DIALOG_DATA = new InjectionToken<unknown>('DIALOG_DATA');

/** Handle of an open dialog; also injectable inside the dialog component to close it with a result. */
export class DialogRef<R = unknown> {
  private readonly result$ = new Subject<R | undefined>();
  /** Emits the result (or `undefined` when dismissed) once, then completes. */
  public readonly closed: Observable<R | undefined> = this.result$.asObservable();
  /** @internal */
  public dispose: () => void = () => { };

  public close(result?: R): void {
    this.dispose();
    this.result$.next(result);
    this.result$.complete();
  }
}

export interface DialogConfig {
  data?: unknown;
  /** Esc and backdrop clicks close the dialog. Off by default, as the previous UI used static backdrops. */
  dismissable?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Opens a standalone component inside a native `<dialog>` styled as a DaisyUI modal (focus trapping, Esc and the top
 * layer come from the platform). The component lays out its own content and uses `.modal-action` for its buttons.
 */
@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly appRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private readonly injector = inject(Injector);

  public open<C, R = unknown>(component: Type<C>, config: DialogConfig = {}): DialogRef<R> {
    const ref = new DialogRef<R>();
    const dialog = document.createElement('dialog');
    dialog.className = 'modal modal-bottom sm:modal-middle';
    const box = document.createElement('div');
    box.className = `modal-box ${config.size === 'lg' ? 'sm:max-w-2xl' : config.size === 'sm' ? 'sm:max-w-sm' : 'sm:max-w-lg'}`;
    dialog.appendChild(box);
    if (config.dismissable) {
      const backdrop = document.createElement('div');
      backdrop.className = 'modal-backdrop';
      backdrop.addEventListener('click', () => ref.close());
      dialog.appendChild(backdrop);
    }
    document.body.appendChild(dialog);

    const elementInjector = Injector.create({
      providers: [
        { provide: DIALOG_DATA, useValue: config.data },
        { provide: DialogRef, useValue: ref }
      ],
      parent: this.injector
    });
    const componentRef = createComponent(component, { environmentInjector: this.environmentInjector, elementInjector });
    box.appendChild(componentRef.location.nativeElement);
    this.appRef.attachView(componentRef.hostView);

    dialog.addEventListener('cancel', event => {
      event.preventDefault();
      if (config.dismissable) {
        ref.close();
      }
    });
    ref.dispose = () => {
      dialog.close();
      this.appRef.detachView(componentRef.hostView);
      componentRef.destroy();
      dialog.remove();
    };
    componentRef.changeDetectorRef.detectChanges();
    dialog.showModal();
    return ref;
  }
}
