import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const CAMPAIGN_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./campaigns.component').then(m => m.CampaignsComponent) },
  {
    path: 'add-campaign', data: { breadcrumb: { title: 'Breadcrumb.Create' } },
    loadComponent: () => import('./create/campaign-create.component').then(m => m.CampaignCreateComponent)
  },
  {
    path: ':campaignId', data: { breadcrumb: { title: 'Breadcrumb.Edit' } },
    loadComponent: () => import('./edit/campaign-edit.component').then(m => m.CampaignEditComponent),
    children: [
      { path: '', redirectTo: 'campaign-details', pathMatch: 'full' },
      {
        path: 'campaign-details', data: { breadcrumb: { title: 'Breadcrumb.BasicInformation' } },
        loadComponent: () => import('./edit/details/campaign-edit-details.component').then(m => m.CampaignDetailsEditComponent)
      },
      {
        path: 'campaign-content', data: { breadcrumb: { title: 'Breadcrumb.Content' } },
        loadComponent: () => import('./edit/content/campaign-edit-content.component').then(m => m.CampaignContentEditComponent)
      },
      {
        path: 'campaign-reports', data: { breadcrumb: { title: 'Breadcrumb.Reports' } },
        loadComponent: () => import('./edit/reports/campaign-reports.component').then(m => m.CampaignReportsComponent)
      },
      {
        path: 'campaign-messages', data: { breadcrumb: { title: 'Breadcrumb.History' } },
        loadComponent: () => import('./edit/messages/campaign-messages.component').then(m => m.CampaignMessagesComponent)
      },
      {
        path: 'campaign-message-timeline/:contactId', data: { breadcrumb: { title: 'Breadcrumb.DeliveryInformation' } },
        loadComponent: () => import('./edit/messages/timeline/campaign-message-timeline.component').then(m => m.CampaignMessageTimelineComponent)
      }
    ]
  }
];

export const CAMPAIGN_PANE_ROUTES: Routes = [
  paneRoute('edit-campaign', () => import('./edit/details/rightpane/campaign-edit-details-rightpane.component').then(m => m.CampaignDetailsEditRightpaneComponent)),
  paneRoute('edit-campaign-attachments', () => import('./edit/details/rightpane/campaign-edit-attachments-rightpane.component').then(m => m.CampaignAttachmentsEditRightpaneComponent))
];
