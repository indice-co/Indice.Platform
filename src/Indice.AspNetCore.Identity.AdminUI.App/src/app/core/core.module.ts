import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';
import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { AuthInterceptor } from './services/auth.interceptor';
import { AuthRenewComponent } from './components/auth-renew/auth-renew.component';
import { CoreRoutingModule } from './core-routing.module';
import { IDENTITY_API_BASE_URL } from './services/identity-api.service';
import { LayoutModule } from '../layout/layout.module';
import * as app from './models/settings';

@NgModule({
    declarations: [
        AuthCallbackComponent,
        AuthRenewComponent
    ],
    imports: [
        HttpClientModule,
        CommonModule,
        BrowserModule,
        FormsModule,
        CoreRoutingModule,
        LayoutModule,
        SweetAlert2Module.forRoot()
    ],
    exports: [
        CoreRoutingModule
    ],
    providers: [
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
        { provide: IDENTITY_API_BASE_URL, useFactory: () => app.settings.api_url }
    ]
})
export class CoreModule { }
