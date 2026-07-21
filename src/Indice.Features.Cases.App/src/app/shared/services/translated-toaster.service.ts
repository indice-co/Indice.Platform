import { Injectable } from '@angular/core';
import { ToasterService, ToastType } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';

/**
 * Thin wrapper over {@link ToasterService} that resolves its title/body from ngx-translate
 * keys via {@link TranslateService.instant} before showing the toast.
 *
 * Toasts are fire-once, triggered by user actions that happen after the translation files have
 * loaded, so a synchronous `instant()` is enough — no observable/subscription is needed.
 *
 * `titleKey`/`bodyKey` may also be plain text (e.g. a server-provided `error.detail`); ngx-translate
 * returns the input unchanged when it does not match a known key, so passing such strings is safe.
 */
@Injectable({
  providedIn: 'root'
})
export class TranslatedToasterService {

  constructor(
    private _toaster: ToasterService,
    private _translate: TranslateService
  ) { }

  public show(type: ToastType, titleKey?: string, bodyKey?: string, delay?: number, params?: any): void {
    this._toaster.show(
      type,
      titleKey ? this._translate.instant(titleKey, params) : undefined,
      bodyKey ? this._translate.instant(bodyKey, params) : undefined,
      delay
    );
  }
}
