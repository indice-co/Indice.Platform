import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AuthHttpInterceptor, AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { AppRoutingModule } from './app-routing.module';
import { BeautifyBooleanPipe } from './shared/pipes.services';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignsDetailsComponent } from './features/campaigns/manage/details/campaigns-details.component';
import { CampaignsManageComponent } from './features/campaigns/manage/campaigns-manage.component';
import { CampaignsRemoveComponent } from './features/campaigns/manage/remove/campaigns-remove.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { HomeComponent } from './features/home/home.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { MESSAGES_API_BASE_URL } from './core/services/campaigns-api.services';
import { MessageTypeCreateComponent } from './features/message-types/create/message-type-create.component';
import { MessageTypesComponent } from './features/message-types/message-types.component';
import { PageIllustrationComponent } from './shared/components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './shared/components/radio-buttons-list/radio-buttons-list.component';
import { ShellConfig } from './shell.config';
import { ToggleButtonComponent } from './shared/components/toggle-button/toggle-button.component';
import * as app from 'src/app/core/models/settings';

@NgModule({
  declarations: [
    AppComponent,
    BeautifyBooleanPipe,
    CampaignCreateComponent,
    CampaignsComponent,
    CampaignsDetailsComponent,
    CampaignsManageComponent,
    CampaignsRemoveComponent,
    DashboardComponent,
    HomeComponent,
    LogOutComponent,
    MessageTypesComponent,
    MessageTypeCreateComponent,
    PageIllustrationComponent,
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
    { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
    { provide: MESSAGES_API_BASE_URL, useFactory: () => app.settings.api_url },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
