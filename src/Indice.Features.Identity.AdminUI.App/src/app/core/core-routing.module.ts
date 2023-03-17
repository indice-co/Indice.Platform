import { NgModule } from '@angular/core';
import { RouterModule, Routes, NoPreloading } from '@angular/router';

import { AuthGuardService } from './services/auth-guard.service';
import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { AuthRenewComponent } from './components/auth-renew/auth-renew.component';
import { PublicShellComponent } from '../layout/components/public-shell/public-shell.component';
import { DashboardShellComponent } from '../layout/components/dashboard-shell/dashboard-shell.component';
import { ErrorComponent } from './components/error/error.component';

const appRoutes: Routes = [
    { path: 'auth-callback', component: AuthCallbackComponent },
    { path: 'auth-renew', component: AuthRenewComponent },
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    {
        path: 'home', component: PublicShellComponent, children: [
            { path: '', loadChildren: () => import('../features/home/home.module').then(x => x.HomeModule) }
        ]
    },
    {
        path: 'app', component: DashboardShellComponent, canActivate: [AuthGuardService], children: [
            { path: 'dashboard', loadChildren: () => import('../features/dashboard/dashboard.module').then(x => x.DashboardModule) },
            { path: 'users', loadChildren: () => import('../features/users/users.module').then(x => x.UsersModule) },
            { path: 'claim-types', loadChildren: () => import('../features/claim-types/claim-types.module').then(x => x.ClaimTypesModule) },
            { path: 'roles', loadChildren: () => import('../features/roles/roles.module').then(x => x.ClaimTypesModule) },
            { path: 'clients', loadChildren: () => import('../features/clients/clients.module').then(x => x.ClientsModule) },
            { path: 'resources', loadChildren: () => import('../features/resources/resources.module').then(x => x.ResourcesModule) },
            { path: 'settings', loadChildren: () => import('../features/settings/settings.module').then(x => x.SettingsModule) },
            { path: 'sign-in-logs', loadChildren: () => import('../features/sign-in-logs/sign-in-logs.module').then(x => x.SignInLogsModule) }
        ]
    },
    {
        path: 'forbidden', component: PublicShellComponent, children: [
            { path: '', component: ErrorComponent, data: { statusCode: 403 } }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forRoot(appRoutes, { preloadingStrategy: NoPreloading, relativeLinkResolution: 'legacy' })],
    exports: [RouterModule]
})
export class CoreRoutingModule { }
