import { Component, inject } from '@angular/core';
import { TenantService } from '@indice/ng-auth';
import { TranslateService } from '@ngx-translate/core';
import { settings } from 'src/app/core/models/settings';
import { IAppSettings } from './core/models/settings.model';

@Component({
  selector: 'app-root',
  template: `
    <lib-shell-layout [sidebarFooterTemplate]="sidebarFooter"></lib-shell-layout>
    <ng-template #sidebarFooter>
      <span>
        Powered by <a class="text-blue-400" href="https://www.indice.gr">Indice</a>
        <span class="ml-1" style="color: red">♥</span> 
      </span> v{{ settings.version }}
    </ng-template>
  `
})
export class AppComponent {
  private _translate = inject(TranslateService)
  constructor(tenantService: TenantService, private translate: TranslateService) {
    if (settings.tenantId && settings.tenantId !== '') {
      tenantService.storeTenant(settings.tenantId);
    }
    const selectedCulture = sessionStorage.getItem('culture') || 'el';
    this.translate.setDefaultLang(selectedCulture);
    this.translate.use(selectedCulture);
  }

  public settings: IAppSettings = settings;
}
