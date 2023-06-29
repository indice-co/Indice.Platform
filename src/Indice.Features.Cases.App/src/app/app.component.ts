import { TranslateService } from '@ngx-translate/core';
import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: '<lib-shell-layout></lib-shell-layout>'
})
export class AppComponent {
  constructor(translate: TranslateService) {
    translate.setDefaultLang('el');
  }
}
