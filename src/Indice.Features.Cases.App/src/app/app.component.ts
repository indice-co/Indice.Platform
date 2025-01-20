import { TranslateService } from '@ngx-translate/core';
import { Component } from '@angular/core';
import { LoadingBarService } from './core/services/loading-bar.service';

@Component({
  selector: 'app-root',

  template: `
    <lib-progress-bar
    style="left: 16rem; position: fixed; width: calc(100% - 16rem); top: 0px; z-index: 200;"
    [busy]="(loadingBarService.busy$ | async) ?? false"
    [value]="(loadingBarService.value$ | async) ?? 0"
    [total]="(loadingBarService.total$ | async) ?? 100"
    [showBarOnly]="true"
    text="Loading...">
    </lib-progress-bar>
    <lib-shell-layout></lib-shell-layout>
  `
})
export class AppComponent {
  constructor(translate: TranslateService, public loadingBarService: LoadingBarService) {
    translate.setDefaultLang('el');
  }
}
