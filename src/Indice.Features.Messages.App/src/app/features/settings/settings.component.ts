import { Component, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideImages, LucideMail } from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';
import { EmailSettingsComponent } from './email/email-settings.component';
import { MediaSettingsComponent } from './media/media-settings.component';

type SettingsTab = 'email' | 'media';

@Component({
  selector: 'app-settings',
  imports: [TranslatePipe, PageHeaderComponent, EmailSettingsComponent, MediaSettingsComponent, LucideImages, LucideMail],
  template: `
    <app-page-header [title]="'Settings.Title' | translate" [subtitle]="'Settings.Subtitle' | translate" />

    @if (enableMediaLibrary) {
      <div role="tablist" class="tabs tabs-border mb-6 border-b border-base-300">
        <button type="button" role="tab" class="tab gap-2" [class.tab-active]="tab() === 'email'" [attr.aria-selected]="tab() === 'email'" (click)="tab.set('email')">
          <svg lucideMail class="size-4"></svg>{{ 'Settings.EmailSendersHeader' | translate }}
        </button>
        <button type="button" role="tab" class="tab gap-2" [class.tab-active]="tab() === 'media'" [attr.aria-selected]="tab() === 'media'" (click)="tab.set('media')">
          <svg lucideImages class="size-4"></svg>{{ 'Settings.MediaLibraryHeader' | translate }}
        </button>
      </div>
    }

    @if (tab() === 'media' && enableMediaLibrary) {
      <app-media-settings />
    } @else {
      <app-email-settings />
    }
  `
})
export class SettingsComponent {
  protected readonly enableMediaLibrary = settings.enableMediaLibrary;
  protected readonly tab = signal<SettingsTab>('email');
}
