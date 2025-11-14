import { LOCALE_ID, NgModule, Provider } from '@angular/core';
import { CommonModule, DatePipe, JsonPipe, registerLocaleData } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS, withInterceptors, provideHttpClient, HttpClient } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { APP_LANGUAGES, APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AuthHttpInterceptor, AUTH_SETTINGS, IndiceAuthModule, TenantHeaderInterceptor, TenantService, TENANT_PREFIX_URL } from '@indice/ng-auth';
import { AppComponent } from './app.component';
import { AppLanguagesService } from './shared/services/app-languages.service';
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
import { CampaignDetailsEditRightpaneComponent } from './features/campaigns/edit/details/rightpane/campaign-edit-details-rightpane.component';
import { CampaignEditComponent } from './features/campaigns/edit/campaign-edit.component';
import { CampaignPreviewComponent } from './features/campaigns/create/steps/preview/campaign-preview.component';
import { CampaignRecipientsComponent } from './features/campaigns/create/steps/recipients/campaign-recipients.component';
import { CampaignReportsComponent } from './features/campaigns/edit/reports/campaign-reports.component';
import { CampaignMessagesComponent } from './features/campaigns/edit/messages/campaign-messages.component';
import { CampaignMessageTimelineComponent } from './features/campaigns/edit/messages/timeline/campaign-message-timeline.component';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DistributionListImportContactsComponent } from './features/distribution-lists/import-contacts/distribution-list-import-contacts.component';
import { DistributionListContactCreateComponent } from './features/distribution-lists/edit/contacts/create/distribution-list-contact-create.component';
import { DistributionListContactEditComponent } from './features/distribution-lists/edit/contacts/edit/distribution-list-contact-edit.component';
import { DistributionListContactsComponent } from './features/distribution-lists/edit/contacts/distribution-list-contacts.component';
import { DistributionListCreateComponent } from './features/distribution-lists/create/distribution-list-create.component';
import { DistributionListDetailsEditComponent } from './features/distribution-lists/edit/details/distribution-list-edit-details.component';
import { DistributionListDetailsEditRightpaneComponent } from './features/distribution-lists/edit/details/rightpane/distribution-list-edit-details-rightpane.component';
import { DistributionListEditComponent } from './features/distribution-lists/edit/distribution-list-edit.component';
import { DistributionListsComponent } from './features/distribution-lists/distribution-lists.component';
import { ContactsListComponent } from './features/contacts/contacts-list.component';
import { ContactComponent } from './features/contacts/contact/contact.component';
import { ContactDetailsComponent } from './features/contacts/contact/details/contact-details.component';
import { ContactCampaignsComponent } from './features/contacts/contact/campaigns/contact-campaigns.component';
import { ContactEditComponent } from './features/contacts/contact/details/edit/contact-edit.component';
import { ContactPreferencesComponent } from './features/contacts/contact/preferences/contact-preferences.component';
import { ContactCreateComponent } from './features/contacts/contact/create/contact-create.component';
import { HighlightModule, HIGHLIGHT_OPTIONS } from 'ngx-highlightjs';
import { HomeComponent } from './features/home/home.component';
import { HttpStatusComponent } from './shared/components/http-status/http-status.component';
import { LocalDropDownMenuComponent } from './shared/components/drop-down-menu/drop-down-menu.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { MESSAGES_API_BASE_URL } from './core/services/messages-api.service';
import { MessageTypeCreateComponent } from './features/message-types/create/message-type-create.component';
import { MessageTypeEditComponent } from './features/message-types/edit/message-type-edit.component';
import { MessageTypesComponent } from './features/message-types/message-types.component';
import { PageIllustrationComponent } from './shared/components/page-illustration/page-illustration.component';
import { RadioButtonsListComponent } from './shared/components/radio-buttons-list/radio-buttons-list.component';
import { ListContactCreateComponent } from './shared/components/list-contact-create/list-contact-create.component';
import { SafePipe } from './shared/pipes/safe.pipe';
import { SumPipe } from './shared/pipes/sum.pipe';
import { ShellConfig } from './shell.config';
import { TemplateContentEditComponent } from './features/templates/edit/content/template-edit-content.component';
import { TemplateCreateComponent } from './features/templates/create/template-create.component';
import { TemplateDetailsEditComponent } from './features/templates/edit/details/template-edit-details.component';
import { TemplateDetailsEditRightpaneComponent } from './features/templates/edit/details/rightpane/template-edit-details-rightpane.component';
import { TemplateEditComponent } from './features/templates/edit/template-edit.component';
import { TemplatesComponent } from './features/templates/templates.component';
import { FileUploadComponent } from './shared/components/file-upload/file-upload.component';
import { DoughnutChartComponent } from './shared/components/doughnut-chart/doughnut-chart.component';
import { LineChartComponent } from './shared/components/line-chart/line-chart.component';
import { MultiFileUploadComponent } from './shared/components/multi-file-upload/multi-file-upload.component';
import { CampaignAttachmentsComponent } from './features/campaigns/create/steps/attachments/campaign-attachments.component';
import { CampaignAttachmentsEditRightpaneComponent } from './features/campaigns/edit/details/rightpane/campaign-edit-attachments-rightpane.component';
import * as app from 'src/app/core/models/settings';
import localeGreek from '@angular/common/locales/el';
import { SettingsComponent } from './features/settings/settings.component';
import { EmailSettingsComponent } from './features/settings/email/email-settings.component';
import { EmailSendersCreateComponent } from './features/settings/email/create/email-senders-create.component';
import { EmailSendersEditComponent } from './features/settings/email/edit/email-senders-edit.component';
import { MediaLibraryComponent } from './features/media-library/media-library.component';
import { TreeBreadcrumbComponent } from './features/media-library/tree-breadcrumb/tree-breadcrumb.component';
import { MEDIA_API_BASE_URL } from './core/services/media-api.service';
import { TreeBreadcrumbItemComponent } from './features/media-library/tree-breadcrumb/tree-breadcrumb-item/tree-breadcrumb-item.component';
import { FolderCreateComponent } from './features/media-library/folder-create/folder-create.component';
import { DocumentUploadComponent } from './features/media-library/document-upload/document-upload.component';
import { FolderViewComponent } from './features/media-library/item-views/folder-view/folder-view.component';
import { DocumentEditComponent } from './features/media-library/document-edit/document-edit.component';
import { DocumentEditRightpaneComponent } from './features/media-library/document-edit/rightpane/document-edit-rightpane.component';
import { FolderEditComponent } from './features/media-library/folder-edit/folder-edit.component';
import { ListViewComponent } from './features/media-library/item-views/list-view/list-view.component';
import { ReadOnlyViewComponent } from './features/media-library/item-views/read-only-view/read-only-view.component';
import { MediaSettingsComponent } from './features/settings/media/media-settings.component';
import { MediaSettingEditComponent } from './features/settings/media/edit/media-setting-edit.component';
import { CodeEditorModule } from '@acrodata/code-editor';
import { NgProgressbar } from 'ngx-progressbar';
import { progressInterceptor, NgProgressHttp } from 'ngx-progressbar/http';
import { ContactDuplicatesComponent } from './features/contacts/contact/duplicates/contact-duplicates.component';
import { MessageEventsComponent } from './features/events/message-events.component';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { ApiTranslateLoader } from './shared/services/api-translate-loader'; 

registerLocaleData(localeGreek);
export function ApiLoaderFactory(http: HttpClient) {
  return new ApiTranslateLoader(http);
}

const providers: Provider[] = [
  DatePipe,
  ModalService,
  JsonPipe,
  { provide: APP_LINKS, useClass: AppLinks },
  { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
  { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: BadRequestInterceptor, multi: true },
  { provide: MESSAGES_API_BASE_URL, useFactory: () => app.settings.api_url },
  { provide: MEDIA_API_BASE_URL, useFactory: () => app.settings.api_url },
  { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() },
  { provide: TENANT_PREFIX_URL, useExisting: MESSAGES_API_BASE_URL },
  { provide: LOCALE_ID, useValue: 'el-GR' },
  {
    provide: HIGHLIGHT_OPTIONS,
    useValue: {
      fullLibraryLoader: () => import('highlight.js'),
      lineNumbers: true,
    }
  },
  { provide: APP_LANGUAGES, useClass: AppLanguagesService }
]

if (app.settings.tenantId) {
  providers.push({ provide: HTTP_INTERCEPTORS, useClass: TenantHeaderInterceptor, multi: true, deps: [MESSAGES_API_BASE_URL, TenantService] })
}

@NgModule({
  declarations: [
    AppComponent,
    BasicModalComponent,
    BeautifyBooleanPipe,
    CampaignAttachmentsComponent,
    CampaignAttachmentsEditRightpaneComponent,
    CampaignBasicInfoComponent,
    CampaignContentComponent,
    CampaignContentEditComponent,
    CampaignCreateComponent,
    CampaignDetailsEditComponent,
    CampaignDetailsEditRightpaneComponent,
    CampaignEditComponent,
    CampaignMessagesComponent,
    CampaignMessageTimelineComponent,
    CampaignPreviewComponent,
    CampaignRecipientsComponent,
    CampaignReportsComponent,
    CampaignsComponent,
    ContactCampaignsComponent,
    ContactComponent,
    ContactCreateComponent,
    ContactDetailsComponent,
    ContactDuplicatesComponent,
    ContactEditComponent,
    ContactPreferencesComponent,
    ContactsListComponent,
    DashboardComponent,
    DistributionListContactCreateComponent,
    DistributionListContactEditComponent,
    DistributionListContactsComponent,
    DistributionListCreateComponent,
    DistributionListDetailsEditComponent,
    DistributionListDetailsEditRightpaneComponent,
    DistributionListEditComponent,
    DistributionListImportContactsComponent,
    DistributionListsComponent,
    DocumentEditComponent,
    DocumentEditRightpaneComponent,
    DocumentUploadComponent,
    DoughnutChartComponent,
    EmailSendersCreateComponent,
    EmailSendersEditComponent,
    EmailSettingsComponent,
    FileUploadComponent,
    FolderCreateComponent,
    FolderEditComponent,
    FolderViewComponent,
    HomeComponent,
    HttpStatusComponent,
    LineChartComponent,
    ListContactCreateComponent,
    ListViewComponent,
    LocalDropDownMenuComponent,
    LogOutComponent,
    MediaLibraryComponent,
    MediaSettingEditComponent,
    MediaSettingsComponent,
    MessageEventsComponent,
    MessageTypeCreateComponent,
    MessageTypeEditComponent,
    MessageTypesComponent,
    MultiFileUploadComponent,
    PageIllustrationComponent,
    RadioButtonsListComponent,
    ReadOnlyViewComponent,
    SafePipe,
    SettingsComponent,
    SumPipe,
    TemplateContentEditComponent,
    TemplateCreateComponent,
    TemplateDetailsEditComponent,
    TemplateDetailsEditRightpaneComponent,
    TemplateEditComponent,
    TemplatesComponent,
    TreeBreadcrumbComponent,
    TreeBreadcrumbItemComponent,
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    CodeEditorModule,
    CommonModule,
    FormsModule,
    HighlightModule,
    HttpClientModule,
    IndiceAuthModule,
    IndiceComponentsModule.forRoot(),
    NgProgressbar,
    NgProgressHttp,
    TranslateModule.forRoot(),
    ReactiveFormsModule
  ],
  providers: [
    ...providers,
    {
      provide: TranslateLoader,
      useFactory: ApiLoaderFactory,
      deps: [HttpClient],
    },
    provideHttpClient(withInterceptors([progressInterceptor]))
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
