import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TenantService } from '@indice/ng-auth';
import { NgProgressbar } from 'ngx-progressbar';
import { NgProgressHttp } from 'ngx-progressbar/http';
import { settings } from './core/models/settings';
import { SidePaneHostComponent } from './layout/side-pane-host.component';
import { ToasterComponent } from './ui/toast/toaster.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NgProgressbar, NgProgressHttp, SidePaneHostComponent, ToasterComponent],
  template: `
    <ng-progress ngProgressHttp [flat]="true" [spinner]="false" />
    <router-outlet />
    <app-side-pane-host />
    <app-toaster />
  `
})
export class AppComponent {
  constructor(tenantService: TenantService) {
    if (settings.tenantId) {
      tenantService.storeTenant(settings.tenantId);
    }
  }
}
