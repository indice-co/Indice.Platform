import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';

import { CoreRoutingModule } from './core-routing.module';


@NgModule({
    declarations: [],
    imports: [
        BrowserModule,
        CommonModule,
        CoreRoutingModule,
        FormsModule,
        HttpClientModule
    ],
    exports: [CoreRoutingModule],
    providers: []
})
export class CoreModule { }
