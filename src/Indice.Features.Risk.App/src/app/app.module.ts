import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { AuthHttpInterceptor, AuthService, AUTH_SETTINGS, IndiceAuthModule } from '@indice/ng-auth';
import { APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { AppRoutingModule } from './app-routing.module';
import * as app from './core/models/settings'
import { HomeComponent } from './features/home/home.component';
import { ShellConfig } from './shell.config';
import { FormsModule } from '@angular/forms';
import { RiskEventsComponent } from './features/risk-events/risk-events.component';
import { RiskResultsComponent } from './features/risk-results/risk-results.component';
import { RISK_API_BASE_URL } from './core/services/risk-api.service';
import { NgxJsonViewerModule } from 'ngx-json-viewer';
import { RiskDetailsPageComponent } from './shared/risk-details-page/risk-details-page.component';
import { RulesListComponent } from './features/rules-list/rules-list.component';
import { RuleOptionsPageComponent } from './shared/rule-options-page/rule-options-page.component';

@NgModule({
  declarations: [
    AppComponent,
    RiskEventsComponent,
    RiskResultsComponent,
    HomeComponent,
    RiskDetailsPageComponent,
    RulesListComponent,
    RuleOptionsPageComponent
  ],
  imports: [
    NgxJsonViewerModule,
    AppRoutingModule,
    BrowserModule,
    CommonModule,
    FormsModule,
    HttpClientModule,
    IndiceAuthModule.forRoot(),
    IndiceComponentsModule.forRoot()
  ],
  providers: [
    ModalService,
    AuthService,
    { provide: APP_LINKS, useFactory: (authService: AuthService) => new AppLinks(authService), deps: [AuthService] },
    { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
    { provide: RISK_API_BASE_URL, useFactory: () => app.settings.api_url },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
