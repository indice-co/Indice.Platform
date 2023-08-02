import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthCallbackComponent, AuthRenewComponent } from '@indice/ng-components';
import { AuthGuardService } from '@indice/ng-auth';
import { CampaignContentEditComponent } from './features/campaigns/edit/content/campaign-edit-content.component';
import { CampaignCreateComponent } from './features/campaigns/create/campaign-create.component';
import { CampaignDetailsEditComponent } from './features/campaigns/edit/details/campaign-edit-details.component';
import { CampaignDetailsEditRightpaneComponent } from './features/campaigns/edit/details/rightpane/campaign-edit-details-rightpane.component';
import { CampaignAttachmentsEditRightpaneComponent } from './features/campaigns/edit/details/rightpane/campaign-edit-attachments-rightpane.component';
import { CampaignEditComponent } from './features/campaigns/edit/campaign-edit.component';
import { CampaignReportsComponent } from './features/campaigns/edit/reports/campaign-reports.component';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CommonAppShellConfig } from './shell.config';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DistributionListContactCreateComponent } from './features/distribution-lists/edit/contacts/create/distribution-list-contact-create.component';
import { DistributionListContactEditComponent } from './features/distribution-lists/edit/contacts/edit/distribution-list-contact-edit.component';
import { DistributionListContactsComponent } from './features/distribution-lists/edit/contacts/distribution-list-contacts.component';
import { DistributionListCreateComponent } from './features/distribution-lists/create/distribution-list-create.component';
import { DistributionListDetailsEditComponent } from './features/distribution-lists/edit/details/distribution-list-edit-details.component';
import { DistributionListDetailsEditRightpaneComponent } from './features/distribution-lists/edit/details/rightpane/distribution-list-edit-details-rightpane.component';
import { DistributionListEditComponent } from './features/distribution-lists/edit/distribution-list-edit.component';
import { DistributionListsComponent } from './features/distribution-lists/distribution-lists.component';
import { HomeComponent } from './features/home/home.component';
import { HttpStatusComponent } from './shared/components/http-status/http-status.component';
import { LogOutComponent } from './core/services/logout/logout.component';
import { MessageTypeCreateComponent } from './features/message-types/create/message-type-create.component';
import { MessageTypeEditComponent } from './features/message-types/edit/message-type-edit.component';
import { MessageTypesComponent } from './features/message-types/message-types.component';
import { TemplateContentEditComponent } from './features/templates/edit/content/template-edit-content.component';
import { TemplateCreateComponent } from './features/templates/create/template-create.component';
import { TemplateDetailsEditComponent } from './features/templates/edit/details/template-edit-details.component';
import { TemplateDetailsEditRightpaneComponent } from './features/templates/edit/details/rightpane/template-edit-details-rightpane.component';
import { TemplateEditComponent } from './features/templates/edit/template-edit.component';
import { TemplatesComponent } from './features/templates/templates.component';
import { SettingsComponent } from './features/settings/settings.component';
import { EmailSendersCreateComponent } from './features/settings/email/create/email-senders-create.component';
import { EmailSendersEditComponent } from './features/settings/email/edit/email-senders-edit.component';

const routes: Routes = [
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'auth-renew', component: AuthRenewComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, pathMatch: 'full', data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  {
    path: 'not-found', component: HttpStatusComponent, data: {
      code: '404',
      title: 'Άγνωστη σελίδα',
      message: 'Η σελίδα που ζητήσατε δεν βρέθηκε',
      shell: CommonAppShellConfig
    }
  },
  {
    path: 'forbidden', component: HttpStatusComponent, data: {
      code: '403',
      title: 'Μη εξουσιοδοτημένη πρόσβαση',
      message: 'Παρακαλώ επικοινωνήστε με την υποστήριξη',
      shell: CommonAppShellConfig
    }
  },
  
  {
    path: '', canActivate: [AuthGuardService], children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent, data: { breadcrumb: { title: 'Αρχική', isHome: true }} },
      { 
        path: 'campaigns', 
        data: { breadcrumb: { title: 'Καμπάνιες' }},
        children: [
          { path: '', component: CampaignsComponent, pathMatch: 'full' },
          { path: 'add-campaign', component: CampaignCreateComponent, data: { breadcrumb: { title: 'Δημιουργία' }} },
          {
            path: ':campaignId', component: CampaignEditComponent, 
            data: { breadcrumb: { title: 'Επεξεργασία' }}, 
            children: [
              { path: '', redirectTo: 'campaign-details', pathMatch: 'full' },
              { path: 'campaign-details', component: CampaignDetailsEditComponent, data: { breadcrumb: { title: 'Βασικές Πληροφορίες' }} },
              { path: 'campaign-content', component: CampaignContentEditComponent, data: { breadcrumb: { title: 'Περιεχόμενο' }} },
              { path: 'campaign-reports', component: CampaignReportsComponent, data: { breadcrumb: { title: 'Αναφορές' }} }
            ]
          },
        ]
      },
      { path: 'message-types', component: MessageTypesComponent, data: { breadcrumb: { title: 'Τύποι Μηνυμάτων' }} },
      { 
        path: 'distribution-lists', 
        data: { breadcrumb: { title: 'Λίστες Διανομής' }},
        children: [
          { path: '', component: DistributionListsComponent, pathMatch: 'full' },
          {
            path: ':distributionListId', component: DistributionListEditComponent, 
            data: { breadcrumb: { title: 'Επεξεργασία' }}, 
            children: [
              { path: '', redirectTo: 'distribution-list-details', pathMatch: 'full' },
              { path: 'distribution-list-details', component: DistributionListDetailsEditComponent, data: { breadcrumb: { title: 'Βασικές Πληροφορίες' }} },
              { path: 'distribution-list-contacts', component: DistributionListContactsComponent, data: { breadcrumb: { title: 'Περιεχόμενο' }} }
            ]
          },
        ]
      },
      { 
        path: 'templates', 
        data: { breadcrumb: { title: 'Πρότυπα' }},
        children: [
          { path: '', component: TemplatesComponent, pathMatch: 'full' },
          { path: 'add-template', component: TemplateCreateComponent, data: { breadcrumb: { title: 'Δημιουργία' }} },
          {
            path: ':templateId', component: TemplateEditComponent, 
            data: { breadcrumb: { title: 'Επεξεργασία' }}, 
            children: [
              { path: '', redirectTo: 'template-details', pathMatch: 'full' },
              { path: 'template-details', component: TemplateDetailsEditComponent, data: { breadcrumb: { title: 'Βασικές Πληροφορίες' }} },
              { path: 'template-content', component: TemplateContentEditComponent, data: { breadcrumb: { title: 'Περιεχόμενο' }} }
            ]
          },
        ]
      },
      { 
        path: 'settings', 
        data: { breadcrumb: { title: 'Ρυθμίσεις' }}, 
        component: SettingsComponent 
      }
    ]
  },
  { path: 'edit-campaign', canActivate: [AuthGuardService], component: CampaignDetailsEditRightpaneComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-campaign-attachments', canActivate: [AuthGuardService], component: CampaignAttachmentsEditRightpaneComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-distribution-list', canActivate: [AuthGuardService], component: DistributionListDetailsEditRightpaneComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'create-distribution-list', canActivate: [AuthGuardService], component: DistributionListCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'create-contact', canActivate: [AuthGuardService], component: DistributionListContactCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-contact/:contactId', canActivate: [AuthGuardService], component: DistributionListContactEditComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-template', canActivate: [AuthGuardService], component: TemplateDetailsEditRightpaneComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'create-message-type', canActivate: [AuthGuardService], component: MessageTypeCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'create-message-sender', canActivate: [AuthGuardService], component: EmailSendersCreateComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-message-type/:messageTypeId', canActivate: [AuthGuardService], component: MessageTypeEditComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'edit-message-sender/:messageSenderId', canActivate: [AuthGuardService], component: EmailSendersEditComponent, outlet: 'rightpane', pathMatch: 'prefix' },
  { path: 'logout', component: LogOutComponent, data: { shell: { fluid: true, showHeader: false, showFooter: false } } },
  {
    path: '**', component: HttpStatusComponent, data: {
      code: '404',
      title: 'Άγνωστη σελίδα',
      message: 'Η σελίδα που ζητήσατε δεν βρέθηκε',
      shell: CommonAppShellConfig
    }
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
