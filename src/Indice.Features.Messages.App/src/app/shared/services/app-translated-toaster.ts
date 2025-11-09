import { Injectable } from '@angular/core';
import { ToastType, ToasterService } from '@indice/ng-components';
;import { AppLanguagesService } from './app-languages.service';
import { combineLatest, take } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AppTranslatedToaster {
    constructor(
        private toastr: ToasterService,
        private translate: AppLanguagesService
    ) {}

  show(type: ToastType, title?: string, body?: string, delay?: number, paramaters?: any): void {

    const translations = combineLatest([
      this.translate.translateKey(title || '', paramaters),
      this.translate.translateKey(body || '', paramaters)
    ]).pipe(take(1)).subscribe(([title,message]) => {
      this.toastr.show(type, title, message, delay);
    });
  }
}
