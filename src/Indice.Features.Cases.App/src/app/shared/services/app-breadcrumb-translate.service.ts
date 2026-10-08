import { Inject, Injectable } from '@angular/core';
import { APP_LANGUAGES, BreadcrumbContext, IBreadcrumbLabelProcessor } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { AppLanguagesService } from './app-languages.service';

/**
 * Translates route breadcrumb titles through the app's translation store. The shell's breadcrumb
 * component renders the returned {@link Observable} via the async pipe, so titles re-translate live
 * when the user switches language. Routes carry their breadcrumb key in `data.breadcrumb.title`.
 */
@Injectable({
  providedIn: 'root'
})
export class AppBreadcrumbTranslateService implements IBreadcrumbLabelProcessor {
  constructor(@Inject(APP_LANGUAGES) private _lang: AppLanguagesService) { }

  public process(context: BreadcrumbContext): Observable<string> {
    return this._lang.translateKey(context.route?.data?.['breadcrumb']?.title);
  }
}
