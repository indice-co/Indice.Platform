import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { SettingsRoutingModule } from './settings-routing.module';
import { SettingsComponent } from './settings.component';
import { SettingEditComponent } from './edit/setting-edit.component';
import { SettingAddComponent } from './add/setting-add.component';

@NgModule({
    declarations: [
        SettingsComponent,
        SettingEditComponent,
        SettingAddComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        SettingsRoutingModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class SettingsModule { }
