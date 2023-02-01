import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { NgbDropdownModule, NgbModalModule } from '@ng-bootstrap/ng-bootstrap';
import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { AdditionalDetailEditComponent } from './edit/additional-details/edit/additional-detail-edit.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { UserAddComponent } from './add/user-add.component';
import { UserAdditionalDetailsComponent } from './edit/additional-details/user-additional-details.component';
import { UserApplicationsComponent } from './edit/applications/user-applications.component';
import { UserDetailsComponent } from './edit/details/user-details.component';
import { UserDevicesComponent } from './edit/devices/user-devices.component';
import { UserEditComponent } from './edit/user-edit.component';
import { UserLoginsComponent } from './edit/logins/user-logins.component';
import { UserRolesComponent } from './edit/roles/user-roles.component';
import { UsersComponent } from './users.component';
import { UsersRoutingModule } from './users-routing.module';

@NgModule({
    declarations: [
        AdditionalDetailEditComponent,
        UserAddComponent,
        UserAdditionalDetailsComponent,
        UserApplicationsComponent,
        UserDetailsComponent,
        UserDevicesComponent,
        UserEditComponent,
        UserLoginsComponent,
        UserRolesComponent,
        UsersComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        UsersRoutingModule,
        NgbDropdownModule,
        NgbModalModule,
        SharedModule,
        SweetAlert2Module
    ]
})
export class UsersModule { }
