import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { NgxChartsModule } from '@swimlane/ngx-charts';
import { DashboardComponent } from './dashboard.component';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { SignInLocationsMapComponent } from './components/sign-in-locations-map/sign-in-locations-map.component';

@NgModule({
    declarations: [
        DashboardComponent,
        SignInLocationsMapComponent
    ],
    imports: [
        CommonModule,
        DashboardRoutingModule,
        NgxChartsModule
    ]
})
export class DashboardModule { }
