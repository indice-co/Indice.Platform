import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { SignInLogsComponent } from './sign-in-logs.component';
import { SignInLogsRoutingModule } from './sign-in-logs-routing.module';

@NgModule({
    declarations: [
        SignInLogsComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        SignInLogsRoutingModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class SignInLogsModule { }
