import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const CONTACT_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./contacts-list.component').then(m => m.ContactsListComponent) },
  {
    path: ':contactId', data: { breadcrumb: { title: 'Breadcrumb.Edit' } },
    loadComponent: () => import('./contact/contact.component').then(m => m.ContactComponent),
    children: [
      { path: '', redirectTo: 'contact-details', pathMatch: 'full' },
      {
        path: 'contact-details', data: { breadcrumb: { title: 'Breadcrumb.BasicInformation' } },
        loadComponent: () => import('./contact/details/contact-details.component').then(m => m.ContactDetailsComponent)
      },
      {
        path: 'contact-campaigns', data: { breadcrumb: { title: 'Breadcrumb.Campaigns' } },
        loadComponent: () => import('./contact/campaigns/contact-campaigns.component').then(m => m.ContactCampaignsComponent)
      },
      {
        path: 'contact-preferences', data: { breadcrumb: { title: 'Breadcrumb.CommunicationDetails' } },
        loadComponent: () => import('./contact/preferences/contact-preferences.component').then(m => m.ContactPreferencesComponent)
      }
    ]
  }
];

export const CONTACT_PANE_ROUTES: Routes = [
  paneRoute('create-new-contact', () => import('./contact/create/contact-create.component').then(m => m.ContactCreateComponent), 'lg'),
  paneRoute('update-contact/:contactId', () => import('./contact/details/edit/contact-edit.component').then(m => m.ContactEditComponent)),
  paneRoute('get-duplicates/:contactId', () => import('./contact/duplicates/contact-duplicates.component').then(m => m.ContactDuplicatesComponent), 'lg')
];
