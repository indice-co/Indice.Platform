import {
  ApplicationRef, Component, ElementRef, EnvironmentInjector, Injectable, Injector, Type,
  afterNextRender, createComponent, inject, input, viewChild
} from '@angular/core';
import { NgComponentOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, map, take } from 'rxjs';
import { provideCurrentLocale } from '../../core/i18n/app-languages.service';
import { DIALOG_DATA, DialogRef } from './dialog-ref';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

export type DialogSize = 'sm' | 'md' | 'lg' | 'xl';

export interface DialogConfig<D = unknown> {
  data?: D;
  size?: DialogSize;
  /** Allow closing with Escape / backdrop click. Defaults to true. */
  dismissible?: boolean;
}

@Component({
  selector: 'app-dialog-host',
  imports: [NgComponentOutlet, TranslatePipe],
  template: `
    <dialog #dialog class="modal modal-bottom sm:modal-middle" (cancel)="onCancel($event)">
      <div class="modal-box" [class]="sizeClass()">
        <ng-container *ngComponentOutlet="component(); injector: injector()" />
      </div>
      @if (dismissible()) {
        <form method="dialog" class="modal-backdrop"><button type="button" (click)="ref().close()">{{ 'Common.Close' | translate }}</button></form>
      }
    </dialog>
  `
})
class DialogHostComponent {
  public readonly component = input.required<Type<unknown>>();
  public readonly injector = input.required<Injector>();
  public readonly ref = input.required<DialogRef<any>>();
  public readonly size = input<DialogSize>('md');
  public readonly dismissible = input(true);
  private readonly _dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    afterNextRender(() => this._dialog().nativeElement.showModal());
  }

  protected sizeClass(): string {
    return { sm: 'max-w-sm', md: 'max-w-lg', lg: 'max-w-2xl', xl: 'max-w-4xl' }[this.size()];
  }

  protected onCancel(event: Event): void {
    event.preventDefault();
    if (this.dismissible()) {
      this.ref().close();
    }
  }

  public hide(): void {
    this._dialog().nativeElement.close();
  }
}

@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly _appRef = inject(ApplicationRef);
  private readonly _environmentInjector = inject(EnvironmentInjector);

  public open<R = unknown, D = unknown>(component: Type<unknown>, config: DialogConfig<D> = {}): DialogRef<R> {
    const ref = new DialogRef<R>();
    const injector = Injector.create({
      providers: [
        { provide: DialogRef, useValue: ref },
        { provide: DIALOG_DATA, useValue: config.data },
        provideCurrentLocale()
      ],
      parent: this._environmentInjector
    });
    const hostRef = createComponent(DialogHostComponent, { environmentInjector: this._environmentInjector });
    hostRef.setInput('component', component);
    hostRef.setInput('injector', injector);
    hostRef.setInput('ref', ref);
    hostRef.setInput('size', config.size ?? 'md');
    hostRef.setInput('dismissible', config.dismissible ?? true);
    this._appRef.attachView(hostRef.hostView);
    document.body.appendChild(hostRef.location.nativeElement);
    ref._onClose = () => {
      hostRef.instance.hide();
      // let the daisyUI close transition finish before tearing down
      setTimeout(() => {
        this._appRef.detachView(hostRef.hostView);
        hostRef.destroy();
        hostRef.location.nativeElement.remove();
      }, 200);
    };
    return ref;
  }

  /** Opens a confirmation dialog. Emits `true` when confirmed, `false` otherwise. */
  public confirm(data: ConfirmDialogData): Observable<boolean> {
    return this.open<boolean, ConfirmDialogData>(ConfirmDialogComponent, { data, size: 'sm' })
      .closed.pipe(take(1), map(result => result === true));
  }
}

export { DIALOG_DATA, DialogRef } from './dialog-ref';
