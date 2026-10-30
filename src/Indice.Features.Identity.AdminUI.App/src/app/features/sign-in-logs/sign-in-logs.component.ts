import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SignInLogsViewComponent } from './sign-in-logs-view.component';

/** Sign in logs of every user. */
@Component({
  selector: 'app-sign-in-logs',
  imports: [PageHeaderComponent, SignInLogsViewComponent, TranslatePipe],
  template: `
    <app-page-header [title]="'Routes.SignInLogs' | translate" [subtitle]="'SignInLogs.Page.Subtitle' | translate" [count]="view.total()" />
    <app-sign-in-logs-view #view />
  `
})
export class SignInLogsComponent { }
