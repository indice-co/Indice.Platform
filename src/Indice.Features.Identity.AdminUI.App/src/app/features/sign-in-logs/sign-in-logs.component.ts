import { Component } from '@angular/core';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { SignInLogsViewComponent } from './sign-in-logs-view.component';

/** Sign in logs of every user. */
@Component({
  selector: 'app-sign-in-logs',
  imports: [PageHeaderComponent, SignInLogsViewComponent],
  template: `
    <app-page-header title="Sign-in logs" subtitle="Every authentication attempt against the identity server, successful or not." />
    <app-sign-in-logs-view />
  `
})
export class SignInLogsComponent { }
