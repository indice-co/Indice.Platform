import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';

import { AuthHttpInterceptor, AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { APP_LINKS, IndiceComponentsModule, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { ShellConfig } from './shell.config';
import { environment } from 'src/environments/environment'
import { AppRoutingModule } from './app-routing.module';
import { CampaignsComponent } from './features/campaigns/campaigns.component';
import { CampaignUpsertComponent } from './features/campaigns/create/campaign-create.component';

@NgModule({
  declarations: [
    AppComponent,
    CampaignsComponent,
    CampaignUpsertComponent
  ],
  imports: [
    AppRoutingModule,
    BrowserModule,
    CommonModule,
    HttpClientModule,
    IndiceAuthModule.forRoot(),
    IndiceComponentsModule.forRoot()
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: AUTH_SETTINGS, useFactory: () => environment.auth_settings },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() },
    { provide: APP_LINKS, useFactory: () => new AppLinks() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
