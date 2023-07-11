import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { AdditionalDetailEditComponent } from './edit/additional-details/edit/additional-detail-edit.component';
import { UserAddComponent } from './add/user-add.component';
import { UserAdditionalDetailsComponent } from './edit/additional-details/user-additional-details.component';
import { UserApplicationsComponent } from './edit/applications/user-applications.component';
import { UserDetailsComponent } from './edit/details/user-details.component';
import { UserDevicesComponent } from './edit/devices/user-devices.component';
import { UserEditComponent } from './edit/user-edit.component';
import { UserLoginsComponent } from './edit/logins/user-logins.component';
import { UserRolesComponent } from './edit/roles/user-roles.component';
import { UsersComponent } from './users.component';
import { UserSignInLogsComponent } from './edit/sign-in-logs/user-sign-in-logs.component';
import { UiFeaturesGuardService } from 'src/app/core/services/ui-features-guard.service';
import { Features } from 'src/app/core/models/features';

const routes: Routes = [
  { path: '', component: UsersComponent },
  { path: 'add', component: UserAddComponent },
  {
    path: ':id', component: UserEditComponent, children: [
      { path: '', redirectTo: 'details', pathMatch: 'full' },
      { path: 'details', component: UserDetailsComponent },
      { path: 'roles', component: UserRolesComponent },
      { path: 'additional-details', component: UserAdditionalDetailsComponent },
      { path: 'additional-details/:id/edit', component: AdditionalDetailEditComponent },
      { path: 'applications', component: UserApplicationsComponent },
      { path: 'external-logins', component: UserLoginsComponent },
      { path: 'devices', component: UserDevicesComponent },
      { path: 'sign-in-logs', component: UserSignInLogsComponent, canActivate: [UiFeaturesGuardService], data: { feature: Features.SignInLogs } }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UsersRoutingModule { }
