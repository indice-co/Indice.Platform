import { TranslateService } from '@ngx-translate/core';
import { Component } from '@angular/core';
import { settings } from './core/models/settings';

@Component({
  selector: 'app-root',
  template: `
  <ng-progress ngProgressHttp/>
  <lib-shell-layout [sidebarFooterTemplate]="sidebarFooter"></lib-shell-layout>
  <ng-template #sidebarFooter>
  <span>
        Powered by <a class="text-blue-400" href="https://www.indice.gr">Indice</a>
  <span class="ml-1" style="color: red">♥</span>
  </span> v{{ settings.version }}
  </ng-template>
  `,
  standalone: false
})
export class AppComponent {
  settings = settings;

  constructor(translate: TranslateService) {
    translate.setDefaultLang('el');
  }
}
