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
import { environment } from 'src/environments/environment';
import { AppRoutingModule } from './app-routing.module';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { CAMPAIGNS_API_BASE_URL } from './core/services/campaigns-api.services';
import { ToggleButtonComponent } from './shared/components/toggle-button/toggle-button.component';
import { RadioButtonsListComponent } from './shared/components/radio-buttons-list/radio-buttons-list.component';
import { CampaignTypesModalComponent } from './features/campaigns/campaign-types-modal/campaign-types.component';

@NgModule({
  declarations: [
    AppComponent,
    CampaignCreateComponent,
    CampaignsComponent,
    CampaignTypesModalComponent,
    DashboardComponent,
    RadioButtonsListComponent,
    ToggleButtonComponent
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
    { provide: APP_LINKS, useFactory: () => new AppLinks() },
    { provide: AUTH_SETTINGS, useFactory: () => environment.auth_settings },
    { provide: CAMPAIGNS_API_BASE_URL, useFactory: () => environment.campaigns_api_url },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
