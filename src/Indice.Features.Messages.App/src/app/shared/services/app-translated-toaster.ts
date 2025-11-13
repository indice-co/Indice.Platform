import { Inject, Injectable } from '@angular/core';
import { APP_LANGUAGES, ToastType, ToasterService } from '@indice/ng-components';
;import { AppLanguagesService } from './app-languages.service';
import { combineLatest, take } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AppTranslatedToaster {
    constructor(
        private toastr: ToasterService,
        @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
    ) {}

  show(type: ToastType, title?: string, body?: string, delay?: number, paramaters?: any): void {

    combineLatest([
      this._lang.translateKey(title || '', paramaters),
      this._lang.translateKey(body || '', paramaters)
    ]).pipe(take(1)).subscribe(([title,message]) => {
      this.toastr.show(type, title, message, delay);
    });
  }
}
