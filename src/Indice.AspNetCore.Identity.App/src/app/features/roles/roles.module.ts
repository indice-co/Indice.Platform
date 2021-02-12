import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { SharedModule } from 'src/app/shared/shared.module';
import { RolesComponent } from './roles.component';
import { RolesRoutingModule } from './roles-routing.module';
import { RoleAddComponent } from './add/role-add.component';
import { RoleEditComponent } from './edit/role-edit.component';

@NgModule({
    declarations: [
        RolesComponent,
        RoleAddComponent,
        RoleEditComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        RolesRoutingModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class ClaimTypesModule { }
