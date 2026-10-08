import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ActivityLogsComponent } from './activity-logs.component';

const routes: Routes = [
    { path: '', component: ActivityLogsComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ActivityLogsRoutingModule { }
