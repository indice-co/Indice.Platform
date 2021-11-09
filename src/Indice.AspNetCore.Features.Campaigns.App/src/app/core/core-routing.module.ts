import { NgModule } from '@angular/core';
import { RouterModule, Routes, NoPreloading } from '@angular/router';

import { AuthCallbackComponent, AuthRenewComponent, PageNotFoundComponent } from '@indice/ng-components';

const appRoutes: Routes = [
    { path: 'auth-callback', component: AuthCallbackComponent },
    { path: 'auth-renew', component: AuthRenewComponent },
    {
        path: '**',
        component: PageNotFoundComponent,
        data: { shell: { fluid: true, showHeader: false, showFooter: false } },
    }
];

@NgModule({
    imports: [RouterModule.forRoot(appRoutes, { preloadingStrategy: NoPreloading })],
    exports: [RouterModule]
})
export class CoreRoutingModule { }
