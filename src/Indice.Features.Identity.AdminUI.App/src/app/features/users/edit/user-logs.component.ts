import { Component, inject } from '@angular/core';
import { ActivityLogsViewComponent } from 'src/app/features/activity-logs/activity-logs-view.component';
import { SignInLogsViewComponent } from 'src/app/features/sign-in-logs/sign-in-logs-view.component';
import { UserStore } from '../user-store';

/** Sign in logs tab: the shared sign in logs view scoped to the user. */
@Component({
  selector: 'app-user-sign-in-logs',
  imports: [SignInLogsViewComponent],
  template: `<app-sign-in-logs-view [subject]="store.id()" />`
})
export class UserSignInLogsComponent {
  protected readonly store = inject(UserStore);
}

/** Activity logs tab: the shared activity logs view scoped to the user. */
@Component({
  selector: 'app-user-activity-logs',
  imports: [ActivityLogsViewComponent],
  template: `<app-activity-logs-view [subject]="store.id()" />`
})
export class UserActivityLogsComponent {
  protected readonly store = inject(UserStore);
}
