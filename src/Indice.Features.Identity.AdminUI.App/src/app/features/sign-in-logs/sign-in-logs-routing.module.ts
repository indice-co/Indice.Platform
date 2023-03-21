import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { SignInLogsComponent } from './sign-in-logs.component';

const routes: Routes = [
    { path: '', component: SignInLogsComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class SignInLogsRoutingModule { }
