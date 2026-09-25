import { NgModule } from '@angular/core';
import { AppComponent } from './app.component';
import { CoreModule } from './core/core.module';
import { progressInterceptor } from 'ngx-progressbar/http';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi, withXhr } from '@angular/common/http';

@NgModule({
  declarations: [AppComponent],
    imports: [CoreModule],
    providers: [
        provideHttpClient(withXhr(), withInterceptors([progressInterceptor]), withInterceptorsFromDi())
    ],
  bootstrap: [AppComponent]
})
export class AppModule { }
