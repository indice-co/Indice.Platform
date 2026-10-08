import { Inject, Injectable } from '@angular/core';
import { APP_LANGUAGES, IBreadcrumbLabelProcessor, BreadcrumbContext } from '@indice/ng-components';
import { AppLanguagesService } from './app-languages.service';

@Injectable({
    providedIn: 'root'
})
export class AppBreadcrumbTranslateService implements IBreadcrumbLabelProcessor {
  constructor(@Inject(APP_LANGUAGES) private _lang: AppLanguagesService) {

  }

  public process(context: BreadcrumbContext) {
    return this._lang.translateKey(context.route?.data?.breadcrumb?.title);
  }
}
