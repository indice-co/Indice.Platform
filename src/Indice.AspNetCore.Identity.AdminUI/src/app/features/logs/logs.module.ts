import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { SharedModule } from 'src/app/shared/shared.module';
import { LogsComponent } from './logs.component';
import { LogsRoutingModule } from './logs-routing.module';

@NgModule({
    declarations: [
        LogsComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        LogsRoutingModule,
        SharedModule,
        NgbModalModule
    ]
})
export class LogsModule { }
