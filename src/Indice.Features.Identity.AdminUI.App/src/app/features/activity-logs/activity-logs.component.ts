import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { ActivityLogsViewComponent } from './activity-logs-view.component';

/** Activity logs of every user. */
@Component({
  selector: 'app-activity-logs',
  imports: [PageHeaderComponent, ActivityLogsViewComponent, TranslatePipe],
  template: `
    <app-page-header [title]="'Routes.ActivityLogs' | translate" [subtitle]="'ActivityLogs.Page.Subtitle' | translate" [count]="view.total()" />
    <app-activity-logs-view #view />
  `
})
export class ActivityLogsComponent { }
