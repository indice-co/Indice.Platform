import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgProgressbar } from 'ngx-progressbar';
import { NgProgressHttp } from 'ngx-progressbar/http';
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
export class AppComponent { }
