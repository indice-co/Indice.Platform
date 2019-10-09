import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

import { NgProgressModule } from '@ngx-progressbar/core';
import { NgProgressHttpModule } from '@ngx-progressbar/http';
import { NgbDropdownModule, NgbToastModule } from '@ng-bootstrap/ng-bootstrap';
import { PublicShellComponent } from './components/public-shell/public-shell.component';
import { DashboardShellComponent } from './components/dashboard-shell/dashboard-shell.component';
import { TopBarComponent } from './components/dashboard-shell/top-bar/top-bar.component';
import { SideMenuComponent } from './components/dashboard-shell/side-menu/side-menu.component';
import { SharedModule } from '../shared/shared.module';
import { AppToastsComponent } from './components/dashboard-shell/toast/toast.component';

@NgModule({
    declarations: [
        PublicShellComponent,
        DashboardShellComponent,
        TopBarComponent,
        SideMenuComponent,
        AppToastsComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        SharedModule,
        NgbDropdownModule,
        NgProgressModule,
        NgProgressHttpModule,
        NgbToastModule
    ]
})
export class LayoutModule { }
