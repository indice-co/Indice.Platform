import { Component } from '@angular/core';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ActivityLogsViewComponent } from './activity-logs-view.component';

/** Activity logs of every user. */
@Component({
  selector: 'app-activity-logs',
  imports: [PageHeaderComponent, ActivityLogsViewComponent],
  template: `
    <app-page-header title="Activity logs" subtitle="An audit trail of the actions users and administrators performed." />
    <app-activity-logs-view />
  `
})
export class ActivityLogsComponent { }
