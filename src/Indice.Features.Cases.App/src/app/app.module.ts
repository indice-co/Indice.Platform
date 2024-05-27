import { JsonSchemaFormModule } from '@ajsf-extended/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { APP_LINKS, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
import { AppComponent } from './app.component';
import { AppLinks } from './app.links';
import { AppRoutingModule } from './app-routing.module';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import * as app from 'src/app/core/models/settings';
import { HomeComponent } from './features/home/home.component';
import { ShellConfig } from './shell.config';
import { LogOutComponent } from './core/services/logout/logout.component';
import { CASES_API_BASE_URL, CasesApiService } from './core/services/cases-api.service';
import { SharedModule } from './shared/shared.module';
import { NotificationsModule } from './features/notifications/notifications.module';
import { CaseTypesModule } from './features/case-types/case-types.module';
import { AcceptLanguageHttpInterceptor } from './core/services/accept-language-http-interceptor.service';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { CaseTypeService } from './core/services/case-type.service';
import { CasesModule } from './features/cases/cases.module';
import { FormsModule } from '@angular/forms';
import { AUTH_SETTINGS, AuthHttpInterceptor, AuthService, IndiceAuthModule } from '@indice/ng-auth';
import { NgModule } from '@angular/core';

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
    CaseTypesModule,
    NotificationsModule,
    JsonSchemaFormModule,
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: HttpLoaderFactory,
        deps: [HttpClient]
      }
    })
  ],
  providers: [
    ModalService,
    AuthService,
    CasesApiService,
    { provide: APP_LINKS, useFactory: (authService: AuthService, caseTypeService: CaseTypeService) => new AppLinks(authService, caseTypeService), deps: [AuthService, CaseTypeService] },
    { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
    { provide: CASES_API_BASE_URL, useFactory: () => app.settings.api_url },
    { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: AcceptLanguageHttpInterceptor, multi: true },
    { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

export function HttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  let assets = app.settings.i18n_assets;
  if (!assets.endsWith('/')) {
    assets += '/';
  }
  return new TranslateHttpLoader(http, assets, '.json');
}
