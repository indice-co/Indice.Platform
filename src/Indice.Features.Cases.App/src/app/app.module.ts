import { JsonSchemaFormModule } from '@ajsf-extended/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS, withInterceptors, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { APP_LANGUAGES, APP_LINKS, BREADCRUMB_LABEL_RESOLVER, IndiceComponentsModule, ModalService, SHELL_CONFIG } from '@indice/ng-components';
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
import { TranslateModule, TranslateService, provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { CaseTypeService } from './core/services/case-type.service';
import { FormsModule } from '@angular/forms';
import { AUTH_SETTINGS, AuthHttpInterceptor, AuthService, IndiceAuthModule } from '@indice/ng-auth';
import { NgModule } from '@angular/core';
import { CasesModule } from './features/cases/cases.module';
import { AppLanguagesService } from './shared/services/app-languages.service';
import { AppBreadcrumbTranslateService } from './shared/services/app-breadcrumb-translate.service';
import { NgProgressbar } from 'ngx-progressbar';
import { progressInterceptor, NgProgressHttp } from 'ngx-progressbar/http';

@NgModule({ declarations: [
        AppComponent,
        DashboardComponent,
        HomeComponent,
        LogOutComponent,
    ],
    bootstrap: [AppComponent], imports: [AppRoutingModule,
        BrowserModule,
        CommonModule,
        FormsModule,
        IndiceAuthModule.forRoot(),
        IndiceComponentsModule.forRoot(),
        SharedModule,
        CasesModule,
        CaseTypesModule,
        NotificationsModule,
        JsonSchemaFormModule,
        NgProgressbar,
        NgProgressHttp,
        TranslateModule.forRoot()], providers: [
        provideTranslateService({
            loader: provideTranslateHttpLoader({ prefix: `${app.settings.api_url}/cases-i18n.`, useHttpBackend: true }),
            fallbackLang: 'en'
        }),
        ModalService,
        AuthService,
        CasesApiService,
        { provide: APP_LINKS, useFactory: (authService: AuthService, caseTypeService: CaseTypeService, translate: TranslateService, lang: AppLanguagesService) => new AppLinks(authService, caseTypeService, translate, lang), deps: [AuthService, CaseTypeService, TranslateService, AppLanguagesService] },
        { provide: AUTH_SETTINGS, useFactory: () => app.settings.auth_settings },
        { provide: CASES_API_BASE_URL, useFactory: () => app.settings.api_url },
        { provide: HTTP_INTERCEPTORS, useClass: AuthHttpInterceptor, multi: true },
        { provide: HTTP_INTERCEPTORS, useClass: AcceptLanguageHttpInterceptor, multi: true },
        { provide: APP_LANGUAGES, useClass: AppLanguagesService },
        { provide: BREADCRUMB_LABEL_RESOLVER, useClass: AppBreadcrumbTranslateService },
        { provide: SHELL_CONFIG, useFactory: () => new ShellConfig() },
        provideHttpClient(withInterceptors([progressInterceptor])),
        provideHttpClient(withInterceptorsFromDi())
    ] })
export class AppModule { }
