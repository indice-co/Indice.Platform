import { NotificationsComponent } from './notifications.component';
import { JsonSchemaFormModule } from "@ajsf-extended/core";
import { CommonModule } from "@angular/common";
import { HttpClientModule } from "@angular/common/http";
import { NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BrowserModule } from "@angular/platform-browser";
import { RouterModule } from "@angular/router";
import { IndiceComponentsModule } from "@indice/ng-components";
import { SharedModule } from "src/app/shared/shared.module";

@NgModule({
    declarations: [
        NotificationsComponent
    ],
    imports: [
        BrowserModule,
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,
        SharedModule,
        JsonSchemaFormModule,
        IndiceComponentsModule
    ],
    exports: [
        NotificationsComponent
    ]
})
export class NotificationsModule { }