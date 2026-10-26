import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucidePencil, LucideSettings } from '@lucide/angular';
import { MediaSetting } from 'src/app/core/services/media-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { SettingsStore } from '../settings-store.service';

/** Media library section of the settings page. */
@Component({
  selector: 'app-media-settings',
  imports: [RouterLink, TranslatePipe, EmptyStateComponent, LucidePencil, LucideSettings],
  template: `
    <section class="panel">
      <div class="border-b border-base-300 p-5 sm:p-6">
        <h2 class="font-semibold">{{ 'Settings.MediaLibraryHeader' | translate }}</h2>
        <p class="text-sm text-base-content/70">{{ 'Settings.MediaLibraryDescription' | translate }}</p>
      </div>
      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ 'Settings.MediaSettingsKeyColumn' | translate }}</th>
              <th>{{ 'Settings.MediaSettingsValueColumn' | translate }}</th>
              <th class="w-16 text-right"><span class="sr-only">{{ 'Settings.MediaSettingsActionsColumn' | translate }}</span></th>
            </tr>
          </thead>
          <tbody>
            @if (!settings()) {
              @for (row of [1, 2]; track row) {
                <tr><td colspan="3"><div class="skeleton h-4 w-full"></div></td></tr>
              }
            } @else {
              @for (item of settings(); track item.key) {
                <tr class="hover:bg-base-200/60">
                  <td>
                    <a class="link link-hover font-semibold" [routerLink]="pane.link(['edit-media-setting', item.key])">{{ item.key }}</a>
                    @if (item.description) {
                      <p class="text-xs text-base-content/60">{{ item.description }}</p>
                    }
                  </td>
                  <td class="max-w-md break-all">
                    @if (item.value) {
                      <code class="rounded bg-base-200 px-1.5 py-0.5 text-xs">{{ item.value }}</code>
                    } @else {
                      <span class="text-base-content/40">—</span>
                    }
                  </td>
                  <td class="text-right">
                    <span class="tooltip tooltip-left" [attr.data-tip]="'Settings.EditMediaSettingTitle' | translate">
                      <a class="btn btn-ghost btn-xs btn-square" [routerLink]="pane.link(['edit-media-setting', item.key])"
                        [attr.aria-label]="'Settings.EditMediaSettingTitle' | translate">
                        <svg lucidePencil class="size-4"></svg>
                      </a>
                    </span>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3">
                    <app-empty-state [title]="'Shared.ListViewEmptyStateTitle' | translate">
                      <svg icon lucideSettings class="size-5"></svg>
                    </app-empty-state>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
    </section>
  `
})
export class MediaSettingsComponent implements OnInit {
  private readonly _store = inject(SettingsStore);
  protected readonly pane = inject(SidePaneService);
  protected readonly settings = signal<MediaSetting[] | undefined>(undefined);

  constructor() {
    inject(RefreshBus).on('media-settings').pipe(takeUntilDestroyed()).subscribe(() => this.load());
  }

  public ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this._store.listMediaSettings().subscribe({
      next: settings => this.settings.set(settings),
      error: () => this.settings.set([])
    });
  }
}
