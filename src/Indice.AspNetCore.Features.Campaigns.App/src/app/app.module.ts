import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AuthHttpInterceptor, AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { ShellConfig } from './shell.config';
import { environment } from 'src/environments/environment'
import { AppRoutingModule } from './app-routing.module';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { CAMPAIGNS_API_BASE_URL } from './core/services/campaigns-api.services';

@NgModule({
  declarations: [
    AppComponent,
    CampaignsComponent,
    CampaignCreateComponent,
    DashboardComponent
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    CommonModule,
    FormsModule,
    HttpClientModule,
    IndiceAuthModule.forRoot(),
    IndiceComponentsModule.forRoot()
  ],
  providers: [
    ModalService,
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: AUTH_SETTINGS, useFactory: () => environment.auth_settings },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() },
    { provide: APP_LINKS, useFactory: () => new AppLinks() },
    { provide: CAMPAIGNS_API_BASE_URL, useFactory: () => environment.campaigns_api_url }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
