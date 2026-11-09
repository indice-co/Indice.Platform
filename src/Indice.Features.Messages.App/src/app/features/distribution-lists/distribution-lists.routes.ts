import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const DISTRIBUTION_LIST_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./distribution-lists.component').then(m => m.DistributionListsComponent) },
  {
    path: ':distributionListId', data: { breadcrumb: { title: 'Breadcrumb.Edit' } },
    loadComponent: () => import('./edit/distribution-list-edit.component').then(m => m.DistributionListEditComponent),
    children: [
      { path: '', redirectTo: 'distribution-list-details', pathMatch: 'full' },
      {
        path: 'distribution-list-details', data: { breadcrumb: { title: 'Breadcrumb.BasicInformation' } },
        loadComponent: () => import('./edit/details/distribution-list-edit-details.component').then(m => m.DistributionListDetailsEditComponent)
      },
      {
        path: 'distribution-list-contacts', data: { breadcrumb: { title: 'Breadcrumb.Contacts' } },
        loadComponent: () => import('./edit/contacts/distribution-list-contacts.component').then(m => m.DistributionListContactsComponent)
      }
    ]
  }
];

export const DISTRIBUTION_LIST_PANE_ROUTES: Routes = [
  paneRoute('create-distribution-list', () => import('./create/distribution-list-create.component').then(m => m.DistributionListCreateComponent)),
  paneRoute('edit-distribution-list', () => import('./edit/details/rightpane/distribution-list-edit-details-rightpane.component').then(m => m.DistributionListDetailsEditRightpaneComponent)),
  paneRoute('import-contacts', () => import('./import-contacts/distribution-list-import-contacts.component').then(m => m.DistributionListImportContactsComponent)),
  paneRoute('create-contact', () => import('./edit/contacts/create/distribution-list-contact-create.component').then(m => m.DistributionListContactCreateComponent), 'lg'),
  // same form as the contact detail page's edit pane
  paneRoute('edit-contact/:contactId', () => import('../contacts/contact/details/edit/contact-edit.component').then(m => m.ContactEditComponent))
];
