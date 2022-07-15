import { NgModule } from '@angular/core';
import { CommonModule, DatePipe, JsonPipe } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AuthHttpInterceptor, AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { AppRoutingModule } from './app-routing.module';
import { BadRequestInterceptor } from './core/bad-request-interceptor';
import { BasicModalComponent } from './shared/components/basic-modal/basic-modal.component';
import { BeautifyBooleanPipe } from './shared/pipes.services';
import { CampaignBasicInfoComponent } from './features/campaigns/create/steps/basic-info/campaign-basic-info.component';
import { CampaignContentComponent } from './features/campaigns/create/steps/content/campaign-content.component';
import { CampaignContentEditComponent } from './features/campaigns/edit/content/campaign-edit-content.component';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { CampaignDetailsEditComponent } from './features/campaigns/edit/details/campaign-edit-details.component';
import { CampaignEditComponent } from './features/campaigns/edit/campaign-edit.component';
import { CampaignPreviewComponent } from './features/campaigns/create/steps/preview/campaign-preview.component';
import { CampaignRecipientsComponent } from './features/campaigns/create/steps/recipients/campaign-recipients.component';
import { CampaignReportsComponent } from './features/campaigns/edit/reports/campaign-reports.component';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DistributionListContactCreateComponent } from './features/distribution-lists/contacts/create/distribution-list-contact-create.component';
import { DistributionListContactsComponent } from './features/distribution-lists/contacts/distribution-list-contacts.component';
import { DistributionListCreateComponent } from './features/distribution-lists/create/distribution-list-create.component';
import { DistributionListEditComponent } from './features/distribution-lists/edit/distribution-list-edit.component';
import { DistributionListsComponent } from './features/distribution-lists/distribution-lists.component';
import { HomeComponent } from './features/home/home.component';
import { LocalDropDownMenuComponent } from './shared/components/drop-down-menu/drop-down-menu.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { MESSAGES_API_BASE_URL } from './core/services/messages-api.service';
import { MessageTypeCreateComponent } from './features/message-types/create/message-type-create.component';
import { MessageTypeEditComponent } from './features/message-types/edit/message-type-edit.component';
import { MessageTypesComponent } from './features/message-types/message-types.component';
import { PageIllustrationComponent } from './shared/components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './shared/components/radio-buttons-list/radio-buttons-list.component';
import { SafePipe } from './shared/pipes/safe.pipe';
import { ShellConfig } from './shell.config';
import { TemplatesComponent } from './features/templates/templates.component';
import { ToggleButtonComponent } from './shared/components/toggle-button/toggle-button.component';
import * as app from 'src/app/core/models/settings';

@NgModule({
  declarations: [
    AppComponent,
    BasicModalComponent,
    BeautifyBooleanPipe,
    CampaignBasicInfoComponent,
    CampaignContentComponent,
    CampaignContentEditComponent,
    CampaignCreateComponent,
    CampaignDetailsEditComponent,
    CampaignEditComponent,
    CampaignPreviewComponent,
    CampaignRecipientsComponent,
    CampaignReportsComponent,
    CampaignsComponent,
    DashboardComponent,
    DistributionListContactCreateComponent,
    DistributionListContactsComponent,
    DistributionListCreateComponent,
    DistributionListEditComponent,
    DistributionListsComponent,
    HomeComponent,
    LocalDropDownMenuComponent,
    LogOutComponent,
    MessageTypeCreateComponent,
    MessageTypeEditComponent,
    MessageTypesComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    SafePipe,
    TemplatesComponent,
    ToggleButtonComponent
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    CommonModule,
    FormsModule,
    HttpClientModule,
    IndiceAuthModule,
    IndiceComponentsModule.forRoot(),
    ReactiveFormsModule
  ],
  providers: [
    DatePipe,
    ModalService,
    JsonPipe,
    { provide: APP_LINKS, useFactory: () => new AppLinks() },
    { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: BadRequestInterceptor, multi: true },
    { provide: MESSAGES_API_BASE_URL, useFactory: () => app.settings.api_url },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
