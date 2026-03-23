import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { ActivityLogsComponent } from './activity-logs.component';
import { ActivityLogsRoutingModule } from './activity-logs-routing.module';

@NgModule({
    declarations: [
        ActivityLogsComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ActivityLogsRoutingModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class ActivityLogsModule { }
