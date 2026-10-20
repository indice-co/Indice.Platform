import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { SplashComponent } from 'src/app/ui/page/splash.component';

@Component({
  selector: 'app-page-not-found',
  imports: [SplashComponent, RouterLink, NgIcon, TranslatePipe],
  template: `
    <app-splash [busy]="false" [title]="'errors.notFound.title' | translate" [message]="'errors.notFound.message' | translate">
      <a routerLink="/dashboard" class="btn btn-primary"><ng-icon name="lucideArrowLeft" size="1rem" /> {{ 'errors.backToDashboard' | translate }}</a>
    </app-splash>
  `
})
export class PageNotFoundComponent { }

@Component({
  selector: 'app-forbidden',
  imports: [SplashComponent, RouterLink, NgIcon, TranslatePipe],
  template: `
    <app-splash [busy]="false" [title]="'errors.forbidden.title' | translate" [message]="'errors.forbidden.message' | translate">
      <a routerLink="/dashboard" class="btn btn-primary"><ng-icon name="lucideArrowLeft" size="1rem" /> {{ 'errors.backToDashboard' | translate }}</a>
    </app-splash>
  `
})
export class ForbiddenComponent { }
