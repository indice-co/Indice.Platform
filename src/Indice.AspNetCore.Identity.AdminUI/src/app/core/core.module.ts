import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';

import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { AuthRenewComponent } from './components/auth-renew/auth-renew.component';
import { IDENTITY_API_BASE_URL } from './services/identity-api.service';
import { environment } from 'src/environments/environment';
import { AuthInterceptor } from './services/auth.interceptor';
import { CoreRoutingModule } from './core-routing.module';
import { LayoutModule } from '../layout/layout.module';
import { SweetAlert2Module } from '@sweetalert2/ngx-sweetalert2';

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
        { provide: IDENTITY_API_BASE_URL, useFactory: () => environment.api_url }
    ]
})
export class CoreModule { }
