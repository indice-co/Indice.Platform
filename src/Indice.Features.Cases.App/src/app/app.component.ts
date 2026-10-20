import { Component } from '@angular/core';
import { NgProgressbar } from 'ngx-progressbar';
import { NgProgressHttp } from 'ngx-progressbar/http';
import { ShellComponent } from './ui/shell/shell.component';

@Component({
  selector: 'app-root',
  imports: [NgProgressbar, NgProgressHttp, ShellComponent],
  template: `
    <ng-progress ngProgressHttp />
    <app-shell />
  `
})
export class AppComponent { }
