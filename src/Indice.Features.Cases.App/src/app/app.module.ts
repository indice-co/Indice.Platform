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
import { DashboardComponent } from './features/dashboard/dashboard.component';
import * as app from 'src/app/core/models/settings';
import { HomeComponent } from './features/home/home.component';
import { ShellConfig } from './shell.config';
import { LogOutComponent } from './core/services/logout/logout.component';
import { CASES_API_BASE_URL } from './core/services/cases-api.service';
import { SharedModule } from './shared/shared.module';
import { CasesModule } from './features/cases/cases.module';
import { NotificationsModule } from './features/notifications/notifications.module';

@NgModule({
  declarations: [
    AppComponent,
    DashboardComponent,
    HomeComponent,
    LogOutComponent,
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    CommonModule,
    FormsModule,
    HttpClientModule,
    IndiceAuthModule.forRoot(),
    IndiceComponentsModule.forRoot(),
    SharedModule,
    CasesModule,
    NotificationsModule
  ],
  providers: [
    ModalService,
    { provide: APP_LINKS, useFactory: () => new AppLinks() },
    { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
    { provide: CASES_API_BASE_URL, useFactory: () => app.settings.api_url },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
