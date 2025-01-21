import { TranslateService } from '@ngx-translate/core';
import { Component } from '@angular/core';
import { ProgressBarService } from './core/services/progress-bar.service';

@Component({
  selector: 'app-root',
  template: `
  <lib-progress-bar
  style="left: 0; position: fixed; width: 100%; top: 0px; z-index: 200;"
  [busy]="(progressBarService.busy$ | async) ?? false"
  [value]="(progressBarService.value$ | async) ?? 0"
  [total]="(progressBarService.total$ | async) ?? 100"
  text="Loading...">
  </lib-progress-bar>
  <lib-shell-layout></lib-shell-layout>
`
})
export class AppComponent {
  constructor(translate: TranslateService, public progressBarService: ProgressBarService) {
    translate.setDefaultLang('el');
  }
}
