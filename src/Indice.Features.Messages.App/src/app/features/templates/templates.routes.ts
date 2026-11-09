import { Routes } from '@angular/router';
import { paneRoute } from 'src/app/ui/side-pane/side-pane';

export const TEMPLATE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./templates.component').then(m => m.TemplatesComponent) },
  {
    path: 'add-template', data: { breadcrumb: { title: 'Breadcrumb.Create' } },
    loadComponent: () => import('./create/template-create.component').then(m => m.TemplateCreateComponent)
  },
  {
    path: ':templateId', data: { breadcrumb: { title: 'Breadcrumb.Edit' } },
    loadComponent: () => import('./edit/template-edit.component').then(m => m.TemplateEditComponent),
    children: [
      { path: '', redirectTo: 'template-details', pathMatch: 'full' },
      {
        path: 'template-details', data: { breadcrumb: { title: 'Breadcrumb.BasicInformation' } },
        loadComponent: () => import('./edit/details/template-edit-details.component').then(m => m.TemplateDetailsEditComponent)
      },
      {
        path: 'template-content', data: { breadcrumb: { title: 'Breadcrumb.Content' } },
        loadComponent: () => import('./edit/content/template-edit-content.component').then(m => m.TemplateContentEditComponent)
      }
    ]
  }
];

export const TEMPLATE_PANE_ROUTES: Routes = [
  // `edit-template/:templateId;section=name|preferences|messageType`
  paneRoute('edit-template/:templateId', () => import('./edit/details/rightpane/template-edit-details-rightpane.component').then(m => m.TemplateDetailsEditRightpaneComponent))
];
